using Finora.Api.DTOs;
using Finora.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Finora.Api.Controllers;

// 통계 API. 로그인해야 호출할 수 있다. (fallback policy)
[ApiController]
[Route("api/statistics")]
public class StatisticsController(StatisticsService statistics) : ControllerBase
{
    // GET /api/statistics/summary?from=2026-10-01&to=2026-10-31 — 기간 수입·지출·합계 + to 기준 누적 총자산
    // DateOnly는 주소의 "2026-10-01" 형식을 그대로 받는다. 날짜 형식이 틀리거나 값이 없으면 [ApiController]가 400으로 응답한다.
    [HttpGet("summary")]
    public async Task<ActionResult<PeriodSummaryResponse>> GetSummary(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await statistics.GetSummaryAsync(from, to, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // GET /api/statistics/categories?from=2026-10-01&to=2026-10-31 — 기간의 대분류별 지출과 비율 (금액 큰 순)
    // &parentId=5를 붙이면 5번 카테고리 바로 아래 단계별 지출 (눌러서 펼치기, 결정 D23)
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryExpenseResponse>>> GetCategoryExpenses(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        [FromQuery] int? parentId,
        CancellationToken cancellationToken)
    {
        var result = await statistics.GetCategoryExpensesAsync(from, to, parentId, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // GET /api/statistics/categories/top?from&to&top=5 — 기간 지출의 대분류 상위 N개 + "기타" 비중 (위젯 ⑥)
    // int top = 5: 주소에 top이 없으면 5. "categories"와 "categories/top"은 다른 주소라 서로 섞이지 않는다
    [HttpGet("categories/top")]
    public async Task<ActionResult<CategorySharesResponse>> GetCategoryShares(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        [FromQuery] int top = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await statistics.GetCategorySharesAsync(from, to, top, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // GET /api/statistics/categories/monthly?from&to&top=5 — 달마다 대분류 상위 N개 + "기타"의 지출 (위젯 ⑦, 최대 36개월)
    [HttpGet("categories/monthly")]
    public async Task<ActionResult<CategoryMonthlyResponse>> GetCategoryMonthly(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        [FromQuery] int top = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await statistics.GetCategoryMonthlyAsync(from, to, top, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // GET /api/statistics/monthly?from=2026-05-01&to=2026-10-31 — 기간에 걸친 달마다의 수입/지출 (최대 36개월)
    [HttpGet("monthly")]
    public async Task<ActionResult<List<MonthlyTotalResponse>>> GetMonthlyTotals(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await statistics.GetMonthlyTotalsAsync(from, to, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // GET /api/statistics/daily?from=2026-10-01&to=2026-10-31 — 기간의 날마다 수입/지출 + 지출 누적 (최대 366일)
    [HttpGet("daily")]
    public async Task<ActionResult<List<DailyTotalResponse>>> GetDailyTotals(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await statistics.GetDailyTotalsAsync(from, to, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }
}
