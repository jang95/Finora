using Finora.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 현재 로그인한 사용자 정보. [AllowAnonymous]가 없으므로 로그인해야 호출할 수 있다. (fallback policy)
// 화면은 앱 시작 시 이 API로 로그인 상태를 확인한다. (200 = 로그인됨, 401 = 로그인 화면으로)
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    // GET /api/me
    // User = Cookie에서 꺼낸 현재 사용자 정보(Claims). 요청 Body/URL이 아니라 인증 정보에서 가져온다. (CLAUDE.md 규칙)
    // Identity.Name은 UserName이고, 가입 시 UserName = Email로 저장했다.
    [HttpGet]
    public MeResponse Get() => new(User.Identity!.Name!);
}
