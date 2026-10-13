using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// [ApiController]: 이 클래스가 Web API용 Controller임을 표시한다. (입력 검증 실패 시 자동으로 400 응답 등)
// [Route("api/health")]: 이 Controller가 받는 주소.
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    // GET /api/health — 서버가 살아 있는지 확인한다.
    // DB 연결 상태는 EF Core를 붙인 뒤(Phase 3 작업 3) 추가한다.
    [HttpGet]
    public IActionResult Get()
    {
        // Ok(...)는 200 응답이고, 넘긴 객체는 JSON으로 바뀐다 → { "status": "ok" }
        return Ok(new { status = "ok" });
    }
}
