using System.Net;
using System.Net.Http.Json;
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
    private const string Password = "password1";

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

    // ---- 테스트 도우미 ----

    private HttpClient CreateClient() =>
        _server.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    // 초대코드 발급 → 가입 API → 로그인 API. 로그인 Cookie를 가진 client를 돌려준다.
    private async Task<HttpClient> RegisterAndLoginAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var inviteCode = $"TEST-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        await using (var db = CreateDb())
        {
            db.InviteCodes.Add(new InviteCode { Code = inviteCode, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var client = CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new { inviteCode, email, password = Password });
        Assert.Equal(HttpStatusCode.NoContent, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        return client;
    }

    private async Task<string> UserIdOfAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<Dictionary<string, string>>("/api/me");
        await using var db = CreateDb();
        return db.Users.Single(u => u.Email == me!["email"]).Id;
    }

    private FinoraDbContext CreateDb() => _server.Services.CreateScope().ServiceProvider.GetRequiredService<FinoraDbContext>();
}
