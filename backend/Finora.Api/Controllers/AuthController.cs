using Finora.Api.Auth;
using Finora.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 회원가입·로그인 API. 로그인 전에 호출하므로 [AllowAnonymous].
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(RegistrationService registration) : ControllerBase
{
    // POST /api/auth/register — 초대코드로 가입한다. 가입 후 자동 로그인하지 않는다. (화면에서 로그인으로 이동)
    // [FromBody]: 요청 본문(JSON)을 RegisterRequest로 바꿔 받는다. 형식이 틀리면 여기 오기 전에 400으로 끝난다.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var errors = await registration.RegisterAsync(request, cancellationToken);
        if (errors.Count == 0)
            return NoContent(); // 204: 성공, 돌려줄 내용 없음

        // 입력 검증 실패와 같은 400 형식(ValidationProblemDetails)으로 응답한다. 화면은 errors를 입력칸 옆에 보여 준다.
        foreach (var (field, messages) in errors)
            foreach (var message in messages)
                ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }
}
