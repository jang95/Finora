using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// 기간 요약 API (Phase 6 완료 기준: "통계 숫자가 가계부 데이터와 일치")
public class StatisticsApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    private record CategoryDto(int Id, string Name, string Type);

    private record SummaryDto(string From, string To, decimal Income, decimal Expense, decimal Balance, decimal TotalAssets);

    [Fact]
    public async Task Summary_SumsPeriodAndAccumulatesTotalAssetsUntilTo()
    {
        var (client, food, salary) = await NewUserAsync();
        // 9월 (기간 전): 급여 3,000,000 / 식비 500,000.25 → 누적 총자산에만 들어간다
        await CreateAsync(client, "2026-09-25", "Income", salary, 3000000);
        await CreateAsync(client, "2026-09-30", "Expense", food, 500000.25m);
        // 10월 (기간): 양 끝 날짜 포함
        await CreateAsync(client, "2026-10-01", "Income", salary, 100000);
        await CreateAsync(client, "2026-10-15", "Expense", food, 12000.50m);
        await CreateAsync(client, "2026-10-31", "Expense", food, 8000);
        // 11월 (기간 후): 어디에도 들어가면 안 된다
        await CreateAsync(client, "2026-11-01", "Income", salary, 999999);

        var summary = await SummaryAsync(client, "2026-10-01", "2026-10-31");

        Assert.Equal("2026-10-01", summary.From);
        Assert.Equal("2026-10-31", summary.To);
        Assert.Equal(100000m, summary.Income);
        Assert.Equal(20000.50m, summary.Expense);
        Assert.Equal(79999.50m, summary.Balance);
        // 3,000,000 − 500,000.25 + 100,000 − 12,000.50 − 8,000
        Assert.Equal(2579999.25m, summary.TotalAssets);
    }

    [Fact]
    public async Task Summary_SingleDay_IncludesOnlyThatDay()
    {
        var (client, food, _) = await NewUserAsync();
        await CreateAsync(client, "2026-10-13", "Expense", food, 1000);
        await CreateAsync(client, "2026-10-14", "Expense", food, 2000);
        await CreateAsync(client, "2026-10-15", "Expense", food, 4000);

        var summary = await SummaryAsync(client, "2026-10-14", "2026-10-14");

        Assert.Equal(2000m, summary.Expense);
        Assert.Equal(-3000m, summary.TotalAssets); // 13일 + 14일 지출 (15일은 제외)
    }

    [Fact]
    public async Task Summary_WithNoTransactions_ReturnsZeros()
    {
        var (client, _, _) = await NewUserAsync();

        var summary = await SummaryAsync(client, "2026-10-01", "2026-10-31");

        Assert.Equal(new SummaryDto("2026-10-01", "2026-10-31", 0m, 0m, 0m, 0m), summary);
    }

    [Fact]
    public async Task Summary_FromAfterTo_Returns400OnTo()
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.GetAsync("/api/statistics/summary?from=2026-10-31&to=2026-10-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("to", out _), $"errors에 to가 없습니다: {body}");
    }

    [Theory]
    [InlineData("/api/statistics/summary?from=2026-10-01")]
    [InlineData("/api/statistics/summary?from=2026-13-01&to=2026-10-31")]
    [InlineData("/api/statistics/summary?from=1999-12-31&to=2026-10-31")]
    public async Task Summary_WithInvalidQuery_Returns400(string url)
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- 테스트 도우미 ----

    private async Task<(HttpClient Client, int Food, int Salary)> NewUserAsync()
    {
        var client = await _server.RegisterAndLoginAsync();
        var categories = (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!;
        return (client, categories.Single(c => c.Name == "식비").Id, categories.Single(c => c.Name == "급여").Id);
    }

    private static async Task CreateAsync(HttpClient client, string date, string type, int categoryId, decimal amount)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new { date, type, categoryId, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<SummaryDto> SummaryAsync(HttpClient client, string from, string to) =>
        (await client.GetFromJsonAsync<SummaryDto>($"/api/statistics/summary?from={from}&to={to}"))!;
}
