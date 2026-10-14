using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// 대시보드 API: 최근 거래. 기간별 숫자는 StatisticsApiTests에서 확인한다.
public class DashboardApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    private record CategoryDto(int Id, string Name, string Type);

    private record TransactionDto(int Id, string Date, string Type, int? CategoryId, string? CategoryName, decimal Amount, string? Memo);

    private record DashboardDto(List<TransactionDto> RecentTransactions);

    [Fact]
    public async Task Dashboard_RecentTransactions_NewestFiveWithCategoryName()
    {
        var (client, food) = await NewUserAsync();
        var created = new List<TransactionDto>();
        foreach (var date in new[] { "2026-10-01", "2026-10-03", "2026-10-03", "2026-10-05", "2026-10-07", "2026-10-09" })
            created.Add(await CreateAsync(client, date, food));

        var recent = (await DashboardAsync(client)).RecentTransactions;

        // 6건 중 최신 5건. 날짜가 같은 10-03 두 건은 나중에 등록한 것(created[2])이 위. 가장 오래된 10-01은 빠진다
        Assert.Equal([created[5].Id, created[4].Id, created[3].Id, created[2].Id, created[1].Id], recent.Select(t => t.Id));
        Assert.All(recent, t => Assert.Equal("식비", t.CategoryName));
    }

    [Fact]
    public async Task Dashboard_WithNoTransactions_ReturnsEmptyList()
    {
        var (client, _) = await NewUserAsync();

        var dashboard = await DashboardAsync(client);

        Assert.Empty(dashboard.RecentTransactions);
    }

    // ---- 테스트 도우미 ----

    private async Task<(HttpClient Client, int Food)> NewUserAsync()
    {
        var client = await _server.RegisterAndLoginAsync();
        var categories = (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!;
        return (client, categories.Single(c => c.Name == "식비").Id);
    }

    private static async Task<TransactionDto> CreateAsync(HttpClient client, string date, int categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new { date, type = "Expense", categoryId, amount = 1000 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
    }

    private static async Task<DashboardDto> DashboardAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
}
