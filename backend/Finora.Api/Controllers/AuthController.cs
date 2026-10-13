using Finora.Api.Auth;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finora.Api.Controllers;

// 회원가입·로그인 API. 로그인 전에 호출하므로 [AllowAnonymous].
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(RegistrationService registration, SignInManager<FinoraUser> signInManager) : ControllerBase
{
    // 로그인 요청 횟수 제한 정책 이름 (Program.cs의 AddRateLimiter에서 정의)
    public const string LoginRateLimitPolicy = "login";

    // POST /api/auth/login — 성공하면 인증 Cookie를 발급한다.
    // [EnableRateLimiting]: 같은 IP에서 짧은 시간에 너무 많이 시도하면 429로 거절한다. (비밀번호 무차별 대입 방지)
    [HttpPost("login")]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // UserName = Email로 가입했으므로 이메일로 로그인한다.
        // isPersistent: 로그인 유지 여부. lockoutOnFailure: 실패 횟수를 세고 5회면 잠근다. (Program.cs의 Lockout 설정)
        var result = await signInManager.PasswordSignInAsync(
            request.Email.Trim(), request.Password, isPersistent: request.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return NoContent(); // Cookie는 응답 헤더(Set-Cookie)로 전달된다

        // 이메일이 없는 경우와 비밀번호가 틀린 경우를 같은 문구로 응답한다. (가입된 이메일인지 알려주지 않음)
        var detail = result.IsLockedOut
            ? "로그인에 여러 번 실패해 계정이 잠겼습니다. 15분 후에 다시 시도하세요."
            : "이메일 또는 비밀번호가 올바르지 않습니다.";
        return Problem(detail: detail, statusCode: StatusCodes.Status401Unauthorized);
    }

    // POST /api/auth/logout — 인증 Cookie를 지운다.
    // 클래스에 [AllowAnonymous]가 있으므로 로그인하지 않은 상태로 호출해도 204다. (지울 Cookie가 없을 뿐, 결과는 같다)
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

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
