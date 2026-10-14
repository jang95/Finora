using Finora.Api.Auth;
using Finora.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finora.Api.Controllers;

// 현재 로그인한 사용자 정보. [AllowAnonymous]가 없으므로 로그인해야 호출할 수 있다. (fallback policy)
// 화면은 앱 시작 시 이 API로 로그인 상태를 확인한다. (200 = 로그인됨, 401 = 로그인 화면으로)
[ApiController]
[Route("api/me")]
public class MeController(PasswordService passwords) : ControllerBase
{
    // GET /api/me
    // User = Cookie에서 꺼낸 현재 사용자 정보(Claims). 요청 Body/URL이 아니라 인증 정보에서 가져온다. (CLAUDE.md 규칙)
    // Identity.Name은 UserName이고, 가입 시 UserName = Email로 저장했다.
    [HttpGet]
    public MeResponse Get() => new(User.Identity!.Name!);

    // POST /api/me/password — 비밀번호 변경. 성공하면 204, 다른 기기의 로그인은 끊긴다.
    // 현재 비밀번호를 계속 바꿔 넣어 보는 시도를 막으려고 로그인과 같은 요청 횟수 제한을 건다.
    [HttpPost("password")]
    [EnableRateLimiting(AuthController.LoginRateLimitPolicy)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var errors = await passwords.ChangeAsync(request);
        if (errors.Count == 0) return NoContent();
        return this.ValidationErrors(errors);
    }
}
