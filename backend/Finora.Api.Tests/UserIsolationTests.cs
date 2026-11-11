using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 사용자 분리 자동 테스트 (Phase 4 완료 기준, 계획서 9장).
// 사용자 A와 B를 실제 가입 API로 만들고, 각자 로그인한 client로 서로의 데이터가 섞이지 않는지 확인한다.
// 사용자 소유 API가 늘어날 때마다(Phase 5~) 이 클래스에 같은 방식의 테스트를 추가한다.
public class UserIsolationTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    // Frontend와 같은 모양으로 응답을 받는다. (enum은 문자열)
    private record CategoryDto(int Id, string Name, string Type);

    [Fact]
    public async Task Categories_EachUserSeesOnlyOwnCategories()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();

        var categoriesA = await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var categoriesB = await clientB.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        // 둘 다 기본 카테고리를 같은 개수로 받지만, Id는 하나도 겹치지 않아야 한다. (각자 자기 행만 받음)
        var expectedCount = DefaultCategories.CreateFor("any").Count;
        Assert.Equal(expectedCount, categoriesA!.Count);
        Assert.Equal(expectedCount, categoriesB!.Count);
        Assert.Empty(categoriesA.Select(c => c.Id).Intersect(categoriesB.Select(c => c.Id)));
        Assert.Contains(categoriesA, c => c.Name == "식비" && c.Type == "Expense");
    }

    [Fact]
    public async Task Categories_ShowsNewCategoryOnlyToOwner()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var userIdA = await UserIdOfAsync(clientA);

        // A의 카테고리를 DB에 직접 하나 추가한다. (추가 API는 Phase 5)
        await using (var db = CreateDb())
        {
            var now = DateTime.UtcNow;
            db.Categories.Add(new Category
            {
                UserId = userIdA, Name = "A만의 카테고리", Type = CategoryType.Expense, CreatedAt = now, UpdatedAt = now,
            });
            await db.SaveChangesAsync();
        }

        var categoriesA = await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var categoriesB = await clientB.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        Assert.Contains(categoriesA!, c => c.Name == "A만의 카테고리");
        Assert.DoesNotContain(categoriesB!, c => c.Name == "A만의 카테고리");
    }

    [Fact]
    public async Task Categories_IgnoresUserIdSentInRequest()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var userIdA = await UserIdOfAsync(clientA);
        var ownCount = (await clientB.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Count;

        // B가 URL에 A의 UserId를 넣어 보내도 서버는 인증 정보(B)만 본다.
        var response = await clientB.GetFromJsonAsync<List<CategoryDto>>($"/api/categories?userId={userIdA}");

        Assert.Equal(ownCount, response!.Count);
        var idsA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Select(c => c.Id);
        Assert.Empty(response.Select(c => c.Id).Intersect(idsA));
    }

    [Fact]
    public async Task Categories_WhenNotLoggedIn_Returns401()
    {
        var response = await CreateClient().GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Categories_CannotRenameOrDeleteOtherUsersCategory_Returns404()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var categoryOfA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))![0];

        // B가 A의 카테고리 Id로 이름 변경·삭제를 시도한다.
        var rename = await clientB.PutAsJsonAsync($"/api/categories/{categoryOfA.Id}", new { name = "B가 바꿈" });
        var delete = await clientB.DeleteAsync($"/api/categories/{categoryOfA.Id}");

        // 403이 아니라 404: 남의 데이터가 있는지조차 알려주지 않는다. (계획서 9장)
        Assert.Equal(HttpStatusCode.NotFound, rename.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        // A의 카테고리는 그대로 남아 있다.
        var afterA = await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        Assert.Contains(afterA!, c => c.Id == categoryOfA.Id && c.Name == categoryOfA.Name);
    }

    [Fact]
    public async Task Categories_CannotCreateSubcategoryUnderOtherUsersCategory()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var categoryOfA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))![0];

        // B가 A의 카테고리 Id를 상위 카테고리로 보낸다. B에게는 "없는 카테고리"로 보여야 한다.
        var response = await clientB.PostAsJsonAsync("/api/categories", new { name = "B의 하위", parentId = categoryOfA.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("ParentId", out _));
        // A의 목록에도, B의 목록에도 생기지 않았다.
        Assert.DoesNotContain((await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!, c => c.Name == "B의 하위");
        Assert.DoesNotContain((await clientB.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!, c => c.Name == "B의 하위");
    }

    [Fact]
    public async Task Transactions_OtherUserCannotSeeUpdateOrDelete()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var foodA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        var foodB = (await clientB.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        var created = await clientA.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = foodA, amount = 10000 });
        var transactionId = (await created.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>())!["id"].GetInt32();

        // B: 같은 달을 조회해도 A의 거래는 없다
        var monthlyB = await clientB.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=10");
        // B: A의 거래 Id로 수정·삭제 → 404
        var update = await clientB.PutAsJsonAsync($"/api/transactions/{transactionId}",
            new { date = "2026-10-14", type = "Expense", categoryId = foodB, amount = 1 });
        var delete = await clientB.DeleteAsync($"/api/transactions/{transactionId}");

        Assert.Equal(0, monthlyB.GetProperty("days").GetArrayLength());
        Assert.Equal(0m, monthlyB.GetProperty("expense").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        // A의 거래는 그대로
        var monthlyA = await clientA.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=10");
        Assert.Equal(10000m, monthlyA.GetProperty("expense").GetDecimal());
    }

    [Fact]
    public async Task Transactions_CannotUseOtherUsersCategory()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var foodA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;

        // B가 A의 카테고리 Id를 넣어 거래를 등록하려고 한다 → 내 카테고리가 아니므로 거절
        var response = await clientB.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = foodA, amount = 10000 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("CategoryId", out _));
    }

    [Fact]
    public async Task Statistics_SummaryIncludesOnlyOwnTransactions()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var foodA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        await clientA.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = foodA, amount = 10000 });

        // const: 바뀌지 않는 값. 메서드 안에서도 쓸 수 있다. A·B가 같은 주소를 조회하게 한 번만 적는다.
        const string url = "/api/statistics/summary?from=2026-10-01&to=2026-10-31";
        var summaryA = await clientA.GetFromJsonAsync<JsonElement>(url);
        var summaryB = await clientB.GetFromJsonAsync<JsonElement>(url);

        // A의 지출은 A의 기간 합계·누적 총자산에만 들어간다
        Assert.Equal(10000m, summaryA.GetProperty("expense").GetDecimal());
        Assert.Equal(-10000m, summaryA.GetProperty("totalAssets").GetDecimal());
        Assert.Equal(0m, summaryB.GetProperty("expense").GetDecimal());
        Assert.Equal(0m, summaryB.GetProperty("totalAssets").GetDecimal());

        // 카테고리별·월별 통계도 마찬가지
        const string period = "?from=2026-10-01&to=2026-10-31";
        var categoriesB = await clientB.GetFromJsonAsync<JsonElement>("/api/statistics/categories" + period);
        var monthlyB = await clientB.GetFromJsonAsync<JsonElement>("/api/statistics/monthly" + period);
        Assert.Equal(0, categoriesB.GetArrayLength());
        Assert.Equal(0m, monthlyB[0].GetProperty("expense").GetDecimal());
        var categoriesA = await clientA.GetFromJsonAsync<JsonElement>("/api/statistics/categories" + period);
        Assert.Equal(1, categoriesA.GetArrayLength());
    }

    [Fact]
    public async Task Statistics_CannotDrillDownIntoOtherUsersCategory()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var categoryOfA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))![0];

        // B가 A의 카테고리 Id로 "펼치기"를 요청한다. B에게는 없는 카테고리로 보여야 한다.
        var response = await clientB.GetAsync($"/api/statistics/categories?from=2026-10-01&to=2026-10-31&parentId={categoryOfA.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ShowsOnlyOwnRecentTransactions()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var foodA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        await clientA.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = foodA, amount = 10000, memo = "A의 거래" });

        var dashboardA = await clientA.GetFromJsonAsync<JsonElement>("/api/dashboard");
        var dashboardB = await clientB.GetFromJsonAsync<JsonElement>("/api/dashboard");

        // 최근 거래 목록은 금액·메모가 그대로 보이므로 특히 남의 것이 섞이면 안 된다
        Assert.Equal(1, dashboardA.GetProperty("recentTransactions").GetArrayLength());
        Assert.Equal(0, dashboardB.GetProperty("recentTransactions").GetArrayLength());
    }

    [Fact]
    public async Task Statistics_TopExpensesShowsOnlyOwnTransactions()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var foodA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        await clientA.PostAsJsonAsync("/api/transactions", new { date = "2026-10-14", type = "Expense", categoryId = foodA, amount = 10000, memo = "A의 큰 지출" });

        var topB = await clientB.GetFromJsonAsync<JsonElement>("/api/statistics/top-expenses?from=2026-10-01&to=2026-10-31");

        // 거래 목록이 그대로 나가는 API라 특히 남의 것이 섞이면 안 된다
        Assert.Equal(0, topB.GetArrayLength());
    }

    [Fact]
    public async Task Statistics_DailyIncludesOnlyOwnTransactions()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var foodA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        await clientA.PostAsJsonAsync("/api/transactions", new { date = "2026-10-14", type = "Expense", categoryId = foodA, amount = 10000 });

        var dailyB = await clientB.GetFromJsonAsync<JsonElement>("/api/statistics/daily?from=2026-10-14&to=2026-10-14");

        // B의 10월 14일은 A의 지출이 섞이지 않고 0이어야 한다
        Assert.Equal(0m, dailyB[0].GetProperty("expense").GetDecimal());
        Assert.Equal(0m, dailyB[0].GetProperty("cumulativeExpense").GetDecimal());
    }

    [Fact]
    public async Task Savings_OtherUserCannotSeeChangeOrRecordIntoSaving()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var savingA = await CreateFreeSavingAsync(clientA);
        var entryA = await clientA.PostAsJsonAsync($"/api/savings/{savingA}/entries", new { date = "2026-03-01", type = "Deposit", amount = 5000 });
        var entryIdA = (await entryA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var body = new { name = "B가 바꿈", kind = "Free", startDate = "2026-01-01" };
        var entryBody = new { date = "2026-03-02", type = "Deposit", amount = 1 };

        // B: 목록에 A의 저금이 없고, A의 저금·기록 Id로 하는 모든 요청은 404
        var listB = await clientB.GetFromJsonAsync<JsonElement>("/api/savings");
        Assert.Equal(0, listB.GetArrayLength());
        var responses = new[]
        {
            await clientB.GetAsync($"/api/savings/{savingA}"),
            await clientB.PutAsJsonAsync($"/api/savings/{savingA}", body),
            await clientB.PutAsJsonAsync($"/api/savings/{savingA}/status", new { status = "Archived" }),
            await clientB.PostAsJsonAsync($"/api/savings/{savingA}/close", new { type = "Cancel", date = "2026-03-03" }),
            await clientB.PostAsJsonAsync($"/api/savings/{savingA}/entries", entryBody),
            await clientB.PutAsJsonAsync($"/api/savings/{savingA}/entries/{entryIdA}", entryBody),
            await clientB.DeleteAsync($"/api/savings/{savingA}/entries/{entryIdA}"),
            await clientB.DeleteAsync($"/api/savings/{savingA}"),
        };
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));

        // A의 저금·기록은 그대로
        var detailA = await clientA.GetFromJsonAsync<JsonElement>($"/api/savings/{savingA}");
        Assert.Equal("A의 저금", detailA.GetProperty("saving").GetProperty("name").GetString());
        Assert.Equal("Active", detailA.GetProperty("saving").GetProperty("status").GetString());
        Assert.Equal(5000m, detailA.GetProperty("saving").GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task Savings_CannotTouchOtherUsersEntryThroughOwnSaving()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var savingA = await CreateFreeSavingAsync(clientA);
        var savingB = await CreateFreeSavingAsync(clientB);
        var entryA = await clientA.PostAsJsonAsync($"/api/savings/{savingA}/entries", new { date = "2026-03-01", type = "Deposit", amount = 5000 });
        var entryIdA = (await entryA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // B가 자기 저금 주소에 A의 기록 Id를 넣는다 → 기록이 B의 것이 아니므로 404
        var update = await clientB.PutAsJsonAsync($"/api/savings/{savingB}/entries/{entryIdA}", new { date = "2026-03-02", type = "Deposit", amount = 1 });
        var delete = await clientB.DeleteAsync($"/api/savings/{savingB}/entries/{entryIdA}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Savings_InterestCannotUseOtherUsersCategory_AndDefaultIsOwnCategory()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();
        var interestA = (await clientA.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "이자").Id;
        var interestB = (await clientB.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "이자").Id;
        var savingB = await CreateFreeSavingAsync(clientB);

        // B가 A의 "이자" 카테고리 Id를 넣는다 → 내 카테고리가 아니므로 거절
        var withA = await clientB.PostAsJsonAsync($"/api/savings/{savingB}/entries",
            new { date = "2026-03-31", type = "Interest", amount = 100, categoryId = interestA });
        Assert.Equal(HttpStatusCode.BadRequest, withA.StatusCode);
        Assert.True((await withA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("CategoryId", out _));

        // 카테고리를 비우면 B 자신의 "이자" 카테고리가 쓰이고, 거래도 B의 가계부에만 생긴다
        var byDefault = await clientB.PostAsJsonAsync($"/api/savings/{savingB}/entries",
            new { date = "2026-03-31", type = "Interest", amount = 100 });
        Assert.Equal(interestB, (await byDefault.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("categoryId").GetInt32());
        var monthlyA = await clientA.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3");
        var monthlyB = await clientB.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3");
        Assert.Equal(0m, monthlyA.GetProperty("income").GetDecimal());
        Assert.Equal(100m, monthlyB.GetProperty("income").GetDecimal());
    }

    [Fact]
    public async Task DashboardLayouts_EachUserHasOwnLayout()
    {
        var clientA = await RegisterAndLoginAsync();
        var clientB = await RegisterAndLoginAsync();

        // A만 배치를 저장한다 (기간 요약을 숨김)
        await clientA.PutAsJsonAsync("/api/dashboard-layouts/statistics",
            new { items = new[] { new { id = "summary", size = "full", hidden = true } } });

        var layoutA = await clientA.GetFromJsonAsync<JsonElement>("/api/dashboard-layouts/statistics");
        var layoutB = await clientB.GetFromJsonAsync<JsonElement>("/api/dashboard-layouts/statistics");

        // A는 저장한 배치, B는 아직 없음(빈 배열). 같은 주소(statistics)라도 사용자마다 다른 행이다
        Assert.Equal(1, layoutA.GetProperty("items").GetArrayLength());
        Assert.Equal(0, layoutB.GetProperty("items").GetArrayLength());

        // B가 저장해도 A의 배치는 바뀌지 않는다 (덮어쓰기는 자기 행만)
        await clientB.PutAsJsonAsync("/api/dashboard-layouts/statistics",
            new { items = new[] { new { id = "monthlyTotals", size = "half", hidden = false } } });
        layoutA = await clientA.GetFromJsonAsync<JsonElement>("/api/dashboard-layouts/statistics");
        Assert.Equal("summary", layoutA.GetProperty("items")[0].GetProperty("id").GetString());
    }

    // ---- 테스트 도우미 ----

    // 자유저금 하나를 만들고 Id를 돌려준다 (가짜 데이터)
    private static async Task<int> CreateFreeSavingAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/savings", new { name = "A의 저금", kind = "Free", startDate = "2026-01-01" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private HttpClient CreateClient() => _server.CreateHttpsClient();

    private Task<HttpClient> RegisterAndLoginAsync() => _server.RegisterAndLoginAsync();

    private async Task<string> UserIdOfAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<Dictionary<string, string>>("/api/me");
        await using var db = CreateDb();
        return db.Users.Single(u => u.Email == me!["email"]).Id;
    }

    private FinoraDbContext CreateDb() => _server.CreateDb();
}
