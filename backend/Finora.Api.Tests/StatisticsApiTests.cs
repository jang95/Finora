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

    // ---- 카테고리별 지출 ----

    [Fact]
    public async Task Categories_SumsExpensePerCategoryLargestFirstWithRoundedRatio()
    {
        var (client, food, salary) = await NewUserAsync();
        var transport = await CategoryIdAsync(client, "교통");
        await CreateAsync(client, "2026-10-01", "Expense", food, 300);
        await CreateAsync(client, "2026-10-31", "Expense", food, 51);
        await CreateAsync(client, "2026-10-15", "Expense", transport, 49);
        await CreateAsync(client, "2026-10-15", "Income", salary, 999999);    // 수입은 지출 비중에 안 들어간다
        await CreateAsync(client, "2026-11-01", "Expense", transport, 5000);  // 기간 밖

        var expenses = await CategoriesAsync(client, "2026-10-01", "2026-10-31");

        // 식비 351 / 400 = 87.75% → 87.8, 교통 49 / 400 = 12.25% → 12.3
        // (딱 중간(…5)은 올린다. C# 기본 반올림이었다면 12.2가 된다)
        Assert.Equal(
            [new CategoryExpenseDto(food, "식비", 351m, 87.8m, false, false), new CategoryExpenseDto(transport, "교통", 49m, 12.3m, false, false)],
            expenses);
    }

    [Fact]
    public async Task Categories_WithNoExpense_ReturnsEmptyList()
    {
        var (client, _, salary) = await NewUserAsync();
        await CreateAsync(client, "2026-10-15", "Income", salary, 1000);

        Assert.Empty(await CategoriesAsync(client, "2026-10-01", "2026-10-31"));
    }

    // 교통 › 대중교통 › 버스 / 교통 › 택시 계층에서 한 단계씩 펼치기 (결정 D23)
    [Fact]
    public async Task Categories_RollsUpSubcategories_AndDrillsDownOneLevelAtATime()
    {
        var (client, food, _) = await NewUserAsync();
        var transport = await CategoryIdAsync(client, "교통");
        var publicTransit = await CreateCategoryAsync(client, "대중교통", transport);
        var bus = await CreateCategoryAsync(client, "버스", publicTransit);
        var taxi = await CreateCategoryAsync(client, "택시", transport);
        await CreateAsync(client, "2026-10-01", "Expense", bus, 300);
        await CreateAsync(client, "2026-10-02", "Expense", publicTransit, 100); // 대중교통에 직접 (버스·지하철 구분 없이)
        await CreateAsync(client, "2026-10-03", "Expense", taxi, 400);
        await CreateAsync(client, "2026-10-04", "Expense", transport, 200);     // 교통에 직접
        await CreateAsync(client, "2026-10-05", "Expense", food, 1000);

        var roots = await CategoriesAsync(client, "2026-10-01", "2026-10-31");
        var underTransport = await CategoriesAsync(client, "2026-10-01", "2026-10-31", transport);
        var underTransit = await CategoriesAsync(client, "2026-10-01", "2026-10-31", publicTransit);

        // 대분류: 교통 = 300 + 100 + 400 + 200 = 1000 (하위 포함). 금액이 같으면 이름순 (교통 < 식비)
        Assert.Equal(
            [new CategoryExpenseDto(transport, "교통", 1000m, 50m, true, false), new CategoryExpenseDto(food, "식비", 1000m, 50m, false, false)],
            roots);
        // 교통 아래: 택시 400, 대중교통 400(버스 300 + 직접 100), 교통 직접 200. 비율은 교통 합계 1000 기준
        Assert.Equal(
            [
                new CategoryExpenseDto(publicTransit, "대중교통", 400m, 40m, true, false),
                new CategoryExpenseDto(taxi, "택시", 400m, 40m, false, false),
                new CategoryExpenseDto(transport, "교통", 200m, 20m, false, true),
            ],
            underTransport);
        // 대중교통 아래: 버스 300, 대중교통 직접 100. 버스는 더 펼칠 하위 분류가 없다
        Assert.Equal(
            [new CategoryExpenseDto(bus, "버스", 300m, 75m, false, false), new CategoryExpenseDto(publicTransit, "대중교통", 100m, 25m, false, true)],
            underTransit);
    }

    [Fact]
    public async Task Categories_WithUnknownParent_Returns400()
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.GetAsync("/api/statistics/categories?from=2026-10-01&to=2026-10-31&parentId=999999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- 월별 합계 ----

    [Fact]
    public async Task Monthly_ReturnsEveryMonthInRangeAcrossYears()
    {
        var (client, food, salary) = await NewUserAsync();
        await CreateAsync(client, "2026-11-14", "Expense", food, 777);       // from 하루 전 → 제외
        await CreateAsync(client, "2026-11-15", "Expense", food, 100);
        await CreateAsync(client, "2026-11-30", "Income", salary, 1000);
        await CreateAsync(client, "2027-01-10", "Expense", food, 50.5m);
        await CreateAsync(client, "2027-01-11", "Expense", food, 888);       // to 다음 날 → 제외

        var months = await MonthlyAsync(client, "2026-11-15", "2027-01-10");

        // 거래 없는 12월도 0으로 들어 있고, 해를 넘어도 오래된 달부터 순서대로
        Assert.Equal(
            [new MonthlyTotalDto(2026, 11, 1000m, 100m), new MonthlyTotalDto(2026, 12, 0m, 0m), new MonthlyTotalDto(2027, 1, 0m, 50.5m)],
            months);
    }

    [Theory]
    [InlineData("2024-01-01", "2026-12-31", HttpStatusCode.OK)]          // 36개월
    [InlineData("2024-01-01", "2027-01-01", HttpStatusCode.BadRequest)]  // 37개월
    public async Task Monthly_AllowsUpTo36Months(string from, string to, HttpStatusCode expected)
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.GetAsync($"/api/statistics/monthly?from={from}&to={to}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/statistics/categories?from=2026-10-31&to=2026-10-01")]
    [InlineData("/api/statistics/monthly?from=2026-10-31&to=2026-10-01")]
    [InlineData("/api/statistics/categories?to=2026-10-31")]
    public async Task CategoriesAndMonthly_WithInvalidPeriod_Returns400(string url)
    {
        var (client, _, _) = await NewUserAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- 테스트 도우미 ----

    private record CategoryExpenseDto(int CategoryId, string CategoryName, decimal Amount, decimal Ratio, bool HasChildren, bool IsDirect);

    private record MonthlyTotalDto(int Year, int Month, decimal Income, decimal Expense);

    private static async Task<int> CategoryIdAsync(HttpClient client, string name) =>
        (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == name).Id;

    // parentId가 있으면 그 카테고리 바로 아래 단계
    private static async Task<List<CategoryExpenseDto>> CategoriesAsync(HttpClient client, string from, string to, int? parentId = null) =>
        (await client.GetFromJsonAsync<List<CategoryExpenseDto>>(
            $"/api/statistics/categories?from={from}&to={to}" + (parentId is null ? "" : $"&parentId={parentId}")))!;

    private static async Task<int> CreateCategoryAsync(HttpClient client, string name, int parentId)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new { name, parentId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!.Id;
    }

    private static async Task<List<MonthlyTotalDto>> MonthlyAsync(HttpClient client, string from, string to) =>
        (await client.GetFromJsonAsync<List<MonthlyTotalDto>>($"/api/statistics/monthly?from={from}&to={to}"))!;

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
