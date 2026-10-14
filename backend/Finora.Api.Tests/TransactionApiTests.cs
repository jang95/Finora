using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 가계부 거래 API: 등록·수정·삭제 검증과 월별·연간 합계 (Phase 5 완료 기준: "월별·연간 합계가 거래 내역과 일치")
// 다른 사용자 데이터 접근은 UserIsolationTests에서 확인한다.
public class TransactionApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    private record CategoryDto(int Id, string Name, string Type);

    private record TransactionDto(int Id, string Date, string Type, int? CategoryId, string? CategoryName, decimal Amount, string? Memo);

    private record DayDto(string Date, decimal Income, decimal Expense, List<TransactionDto> Transactions);

    private record MonthlyDto(decimal Income, decimal Expense, decimal Balance, List<DayDto> Days);

    private record MonthDto(int Month, decimal Income, decimal Expense, decimal Balance);

    private record YearlyDto(decimal Income, decimal Expense, decimal Balance, List<MonthDto> Months);

    // ---- 등록·수정·삭제 ----

    [Fact]
    public async Task Create_ReturnsTransactionWithCategoryName()
    {
        var (client, food, _) = await NewUserAsync();

        var response = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = food, amount = 12000, memo = "  점심 " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.Equal("2026-10-14", created!.Date);
        Assert.Equal("식비", created.CategoryName);
        Assert.Equal(12000m, created.Amount);
        Assert.Equal("점심", created.Memo); // 앞뒤 공백 제거
    }

    [Fact]
    public async Task Create_WithoutMemo_OmitsMemoInJson()
    {
        var (client, food, _) = await NewUserAsync();

        var response = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = food, amount = 1000, memo = "   " });

        // null인 값은 JSON에 아예 없어야 한다 (Frontend의 memo?: string과 맞춤)
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.TryGetProperty("memo", out _));
    }

    [Theory] // [Theory] + [InlineData]: 같은 테스트를 입력만 바꿔 여러 번 실행한다
    [InlineData(0)]
    [InlineData(-500)]
    [InlineData(1000000000001)]
    [InlineData(10.555)]
    public async Task Create_WithInvalidAmount_Returns400(decimal amount)
    {
        var (client, food, _) = await NewUserAsync();

        var response = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = food, amount });

        await AssertFieldErrorAsync(response, "Amount");
    }

    [Fact]
    public async Task Create_WithMissingFields_Returns400()
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.PostAsJsonAsync("/api/transactions", new { memo = "값 없음" });

        // 응답 본문은 한 번만 읽을 수 있어서, 한 번 읽고 세 칸을 확인한다.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Date", out _));
        Assert.True(errors.TryGetProperty("Type", out _));
        Assert.True(errors.TryGetProperty("Amount", out _));
    }

    [Fact]
    public async Task Create_WithoutCategoryOrWithWrongTypeCategory_Returns400()
    {
        var (client, food, salary) = await NewUserAsync();

        var noCategory = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", amount = 1000 });
        // 지출 거래에 수입 카테고리(급여)
        var wrongType = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = salary, amount = 1000 });
        var unknown = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Expense", categoryId = 999999, amount = 1000 });

        await AssertFieldErrorAsync(noCategory, "CategoryId");
        await AssertFieldErrorAsync(wrongType, "CategoryId");
        await AssertFieldErrorAsync(unknown, "CategoryId");
        _ = food; // 이 테스트에서는 쓰지 않음
    }

    [Fact]
    public async Task Create_Transfer_IsRejectedUntilAccountsExist()
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.PostAsJsonAsync("/api/transactions",
            new { date = "2026-10-14", type = "Transfer", amount = 1000, accountId = 1, toAccountId = 2 });

        await AssertFieldErrorAsync(response, "Type");
    }

    [Fact]
    public async Task Update_ChangesValuesAndMovesToOtherMonth()
    {
        var (client, food, salary) = await NewUserAsync();
        var created = await CreateAsync(client, "2026-10-14", "Expense", food, 5000);

        var response = await client.PutAsJsonAsync($"/api/transactions/{created.Id}",
            new { date = "2026-09-30", type = "Income", categoryId = salary, amount = 3000000 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var october = await MonthlyAsync(client, 2026, 10);
        var september = await MonthlyAsync(client, 2026, 9);
        Assert.Empty(october.Days);
        var moved = Assert.Single(Assert.Single(september.Days).Transactions);
        Assert.Equal("Income", moved.Type);
        Assert.Equal("급여", moved.CategoryName);
        Assert.Equal(3000000m, september.Income);
    }

    [Fact]
    public async Task Delete_RemovesTransaction()
    {
        var (client, food, _) = await NewUserAsync();
        var created = await CreateAsync(client, "2026-10-14", "Expense", food, 5000);

        var response = await client.DeleteAsync($"/api/transactions/{created.Id}");
        var again = await client.DeleteAsync($"/api/transactions/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Empty((await MonthlyAsync(client, 2026, 10)).Days);
    }

    [Fact]
    public async Task DeleteCategory_UsedByTransaction_Returns409()
    {
        var (client, food, _) = await NewUserAsync();
        await CreateAsync(client, "2026-10-14", "Expense", food, 5000);

        var response = await client.DeleteAsync($"/api/categories/{food}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("사용 중", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task DeleteUser_RemovesTheirTransactionsAndCategories()
    {
        var (client, food, _) = await NewUserAsync();
        await CreateAsync(client, "2026-10-14", "Expense", food, 5000);
        var email = (await client.GetFromJsonAsync<Dictionary<string, string>>("/api/me"))!["email"];

        // 사용자 삭제 → 거래·카테고리 Cascade 삭제. (거래 → 카테고리 FK가 NoAction이라 순서 문제 없이 지워져야 한다)
        using var scope = _server.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FinoraUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var result = await userManager.DeleteAsync(user!);

        Assert.True(result.Succeeded);
        await using var db = _server.CreateDb();
        Assert.False(await db.Transactions.IgnoreQueryFilters().AnyAsync(t => t.UserId == user!.Id));
        Assert.False(await db.Categories.IgnoreQueryFilters().AnyAsync(c => c.UserId == user!.Id));
    }

    // ---- 월별·연간 합계 ----

    [Fact]
    public async Task Monthly_GroupsByDateNewestFirstWithDailyAndMonthlyTotals()
    {
        var (client, food, salary) = await NewUserAsync();
        // 10월: 1일 급여 3,000,000 / 1일 식비 12,500.50 / 15일 식비 8,000 + 2,000 / 31일 식비 1,000
        await CreateAsync(client, "2026-10-01", "Income", salary, 3000000);
        await CreateAsync(client, "2026-10-01", "Expense", food, 12500.50m);
        var first15 = await CreateAsync(client, "2026-10-15", "Expense", food, 8000);
        var second15 = await CreateAsync(client, "2026-10-15", "Expense", food, 2000);
        await CreateAsync(client, "2026-10-31", "Expense", food, 1000);
        // 경계 밖: 9월 30일, 11월 1일 → 10월 합계에 들어가면 안 된다
        await CreateAsync(client, "2026-09-30", "Expense", food, 777);
        await CreateAsync(client, "2026-11-01", "Expense", food, 888);

        var october = await MonthlyAsync(client, 2026, 10);

        Assert.Equal(3000000m, october.Income);
        Assert.Equal(23500.50m, october.Expense); // decimal이라 소수 오차가 없다
        Assert.Equal(2976499.50m, october.Balance);
        Assert.Equal(["2026-10-31", "2026-10-15", "2026-10-01"], october.Days.Select(d => d.Date));
        var day15 = october.Days[1];
        Assert.Equal(10000m, day15.Expense);
        Assert.Equal(0m, day15.Income);
        // 같은 날은 나중에 등록한 거래가 위
        Assert.Equal([second15.Id, first15.Id], day15.Transactions.Select(t => t.Id));
        var day1 = october.Days[2];
        Assert.Equal(3000000m, day1.Income);
        Assert.Equal(12500.50m, day1.Expense);
    }

    [Fact]
    public async Task Yearly_ReturnsTwelveMonthsWithTotals()
    {
        var (client, food, salary) = await NewUserAsync();
        await CreateAsync(client, "2026-01-25", "Income", salary, 3000000);
        await CreateAsync(client, "2026-01-31", "Expense", food, 100000);
        await CreateAsync(client, "2026-12-01", "Expense", food, 50000.25m);
        await CreateAsync(client, "2025-12-31", "Income", salary, 999); // 다른 해
        await CreateAsync(client, "2027-01-01", "Income", salary, 999); // 다른 해

        var yearly = await client.GetFromJsonAsync<YearlyDto>("/api/transactions/yearly?year=2026");

        Assert.Equal(12, yearly!.Months.Count);
        Assert.Equal(Enumerable.Range(1, 12), yearly.Months.Select(m => m.Month));
        Assert.Equal(new MonthDto(1, 3000000m, 100000m, 2900000m), yearly.Months[0]);
        Assert.Equal(new MonthDto(6, 0m, 0m, 0m), yearly.Months[5]); // 거래 없는 달은 0
        Assert.Equal(new MonthDto(12, 0m, 50000.25m, -50000.25m), yearly.Months[11]);
        Assert.Equal(3000000m, yearly.Income);
        Assert.Equal(150000.25m, yearly.Expense);
        Assert.Equal(2849999.75m, yearly.Balance);
    }

    [Theory]
    [InlineData("/api/transactions/monthly?year=2026&month=13")]
    [InlineData("/api/transactions/monthly?year=2026")]
    [InlineData("/api/transactions/yearly?year=1999")]
    public async Task Ledger_WithInvalidQuery_Returns400(string url)
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- 테스트 도우미 ----

    // 새 사용자로 로그인하고, 기본 카테고리 중 "식비"(지출)와 "급여"(수입)의 Id를 함께 돌려준다.
    private async Task<(HttpClient Client, int Food, int Salary)> NewUserAsync()
    {
        var client = await _server.RegisterAndLoginAsync();
        var categories = (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!;
        return (client, categories.Single(c => c.Name == "식비").Id, categories.Single(c => c.Name == "급여").Id);
    }

    private static async Task<TransactionDto> CreateAsync(HttpClient client, string date, string type, int categoryId, decimal amount)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new { date, type, categoryId, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
    }

    private static async Task<MonthlyDto> MonthlyAsync(HttpClient client, int year, int month) =>
        (await client.GetFromJsonAsync<MonthlyDto>($"/api/transactions/monthly?year={year}&month={month}"))!;

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors에 {field}가 없습니다: {body}");
    }
}
