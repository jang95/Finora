using Finora.Api.DTOs;
using Finora.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 대시보드 API. 로그인해야 호출할 수 있다. (fallback policy)
[ApiController]
[Route("api/dashboard")]
public class DashboardController(StatisticsService statistics) : ControllerBase
{
    // GET /api/dashboard — 최근 거래 (기간별 수입·지출·총자산은 GET /api/statistics/summary)
    [HttpGet]
    public Task<DashboardResponse> Get(CancellationToken cancellationToken) =>
        statistics.GetDashboardAsync(cancellationToken);
}
