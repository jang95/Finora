using Finora.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// [ApiController]: 이 클래스가 Web API용 Controller임을 표시한다. (입력 검증 실패 시 자동으로 400 응답 등)
// [Route("api/health")]: 이 Controller가 받는 주소.
// 생성자 매개변수 db는 Program.cs의 AddDbContext 등록 덕분에 자동으로 들어온다. (의존성 주입)
[ApiController]
[Route("api/health")]
public class HealthController(FinoraDbContext db) : ControllerBase
{
    // GET /api/health — 서버와 DB 연결 상태를 확인한다.
    // async/await: DB 응답을 기다리는 동안 스레드를 붙잡지 않고 다른 요청을 처리할 수 있게 한다.
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dbConnected = await db.Database.CanConnectAsync(cancellationToken);

        // DB가 안 되면 503(Service Unavailable)로 응답한다. 접속 문자열·오류 내용은 밖으로 내보내지 않는다.
        var body = new { status = dbConnected ? "ok" : "degraded", database = dbConnected ? "connected" : "unavailable" };
        return dbConnected ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
