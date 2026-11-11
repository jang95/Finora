using Finora.Api.DTOs;
using Finora.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 위젯 배치 API (결정 D27). 로그인해야 호출할 수 있다. 규칙은 DashboardLayoutService.
[ApiController]
[Route("api/dashboard-layouts")]
public class DashboardLayoutsController(DashboardLayoutService layouts) : ControllerBase
{
    // GET /api/dashboard-layouts/statistics — 내 배치. 저장한 적이 없으면 items가 빈 배열
    // 모르는 화면 이름이면 404 (그런 주소가 없는 것과 같다)
    [HttpGet("{page}")]
    public async Task<ActionResult<DashboardLayoutResponse>> Get(string page, CancellationToken cancellationToken)
    {
        var result = await layouts.GetAsync(page, cancellationToken);
        if (result.IsNotFound) return NotFound();
        return result.Value!;
    }

    // PUT /api/dashboard-layouts/statistics — 배치 전체 저장 (없으면 만들고 있으면 덮어씀)
    // PUT은 "이 주소의 내용을 이것으로 바꿔라"는 뜻이라, 같은 요청을 여러 번 보내도 결과가 같다 (멱등). 저장은 POST가 아니라 PUT
    [HttpPut("{page}")]
    public async Task<ActionResult<DashboardLayoutResponse>> Save(string page, [FromBody] SaveDashboardLayoutRequest request, CancellationToken cancellationToken)
    {
        var result = await layouts.SaveAsync(page, request, cancellationToken);
        if (result.IsNotFound) return NotFound();
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }
}
