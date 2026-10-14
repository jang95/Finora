using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 대시보드 API (Phase 6 완료 기준: "대시보드 숫자가 가계부 데이터와 일치")
public class DashboardApiTests : IClassFixture<PostgresApiFactory>
{
    // 테스트용 시계. 테스트마다 Now를 원하는 시각으로 정한다.
    private readonly TestClock _clock = new();
    private readonly WebApplicationFactory<Program> _server;

    public DashboardApiTests(PostgresApiFactory factory)
    {
        _server = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:LoginPermitPerMinute", "1000");
            // 앱이 등록한 TimeProvider.System 대신 테스트 시계를 쓰게 한다 (나중에 등록한 것이 이긴다)
            b.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(_clock));
        });
    }

    private record CategoryDto(int Id, string Name, string Type);

    private record TransactionDto(int Id, string Date, string Type, int? CategoryId, string? CategoryName, decimal Amount, string? Memo);

    private record DashboardDto(decimal TotalAssets, decimal MonthlyIncome, decimal MonthlyExpense, List<TransactionDto> RecentTransactions);

    private record MonthlyDto(decimal Income, decimal Expense);

    [Fact]
    public async Task Dashboard_ThisMonthTotalsMatchLedgerAndTotalAssetsIncludesAll()
    {
        _clock.Now = KoreaNoon(2026, 10, 14);
        var (client, food, salary) = await NewUserAsync();
        await CreateAsync(client, "2026-09-25", "Income", salary, 3000000);      // 지난달: 총자산에만
        await CreateAsync(client, "2026-10-01", "Income", salary, 100000);
        await CreateAsync(client, "2026-10-14", "Expense", food, 12000.50m);
        await CreateAsync(client, "2026-10-31", "Expense", food, 8000);          // 이번 달 말일 (오늘 이후여도 이번 달)
        await CreateAsync(client, "2026-11-01", "Expense", food, 500);           // 다음 달: 총자산에만

        var dashboard = await DashboardAsync(client);
        var ledger = (await client.GetFromJsonAsync<MonthlyDto>("/api/transactions/monthly?year=2026&month=10"))!;

        Assert.Equal(100000m, dashboard.MonthlyIncome);
        Assert.Equal(20000.50m, dashboard.MonthlyExpense);
        // 가계부 월 합계와 같은 값
        Assert.Equal(ledger.Income, dashboard.MonthlyIncome);
        Assert.Equal(ledger.Expense, dashboard.MonthlyExpense);
        // 3,000,000 + 100,000 − 12,000.50 − 8,000 − 500
        Assert.Equal(3079499.50m, dashboard.TotalAssets);
    }

    [Fact]
    public async Task Dashboard_RecentTransactions_NewestFiveWithCategoryName()
    {
        _clock.Now = KoreaNoon(2026, 10, 14);
        var (client, food, _) = await NewUserAsync();
        var created = new List<TransactionDto>();
        foreach (var date in new[] { "2026-10-01", "2026-10-03", "2026-10-03", "2026-10-05", "2026-10-07", "2026-10-09" })
            created.Add(await CreateAsync(client, date, "Expense", food, 1000));

        var recent = (await DashboardAsync(client)).RecentTransactions;

        // 6건 중 최신 5건. 날짜가 같은 10-03 두 건은 나중에 등록한 것(created[2])이 위. 가장 오래된 10-01은 빠진다
        Assert.Equal([created[5].Id, created[4].Id, created[3].Id, created[2].Id, created[1].Id], recent.Select(t => t.Id));
        Assert.All(recent, t => Assert.Equal("식비", t.CategoryName));
    }

    [Fact]
    public async Task Dashboard_ThisMonthUsesKoreaTime()
    {
        // UTC 10월 31일 15:30 = 한국 11월 1일 00:30 → "이번 달"은 11월
        _clock.Now = new DateTimeOffset(2026, 10, 31, 15, 30, 0, TimeSpan.Zero);
        var (client, food, _) = await NewUserAsync();
        await CreateAsync(client, "2026-10-31", "Expense", food, 1000);
        await CreateAsync(client, "2026-11-01", "Expense", food, 2000);

        var dashboard = await DashboardAsync(client);

        Assert.Equal(2000m, dashboard.MonthlyExpense);
    }

    [Fact]
    public async Task Dashboard_WithNoTransactions_ReturnsZerosAndEmptyList()
    {
        _clock.Now = KoreaNoon(2026, 10, 14);
        var (client, _, _) = await NewUserAsync();

        var dashboard = await DashboardAsync(client);

        Assert.Equal(0m, dashboard.TotalAssets);
        Assert.Equal(0m, dashboard.MonthlyIncome);
        Assert.Equal(0m, dashboard.MonthlyExpense);
        Assert.Empty(dashboard.RecentTransactions);
    }

    // ---- 테스트 도우미 ----

    // 한국 시간 그날 정오. (한국은 UTC+9 → UTC 03:00)
    private static DateTimeOffset KoreaNoon(int year, int month, int day) => new(year, month, day, 12, 0, 0, TimeSpan.FromHours(9));

    // TimeProvider를 상속해서 "지금"만 바꾼다. override: 부모 클래스의 동작을 이 클래스에서 다시 정의한다.
    private class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    }

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

    private static async Task<DashboardDto> DashboardAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
}
