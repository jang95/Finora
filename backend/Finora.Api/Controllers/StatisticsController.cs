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

    // GET /api/statistics/categories?from=2026-10-01&to=2026-10-31 — 기간의 카테고리별 지출과 비율 (금액 큰 순)
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryExpenseResponse>>> GetCategoryExpenses(
        [FromQuery, BindRequired] DateOnly from,
        [FromQuery, BindRequired] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await statistics.GetCategoryExpensesAsync(from, to, cancellationToken);
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
}
