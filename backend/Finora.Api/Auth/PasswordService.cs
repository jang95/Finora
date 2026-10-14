using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.AspNetCore.Identity;

namespace Finora.Api.Auth;

// 로그인한 사용자의 비밀번호 변경.
// 바꿀 사용자는 요청 값이 아니라 인증 Cookie(CurrentUser)에서 정한다. (CLAUDE.md 규칙)
public class PasswordService(UserManager<FinoraUser> userManager, SignInManager<FinoraUser> signInManager, CurrentUser currentUser)
{
    // 성공하면 빈 목록, 실패하면 "입력칸 이름 → 오류 문구들" (RegistrationService와 같은 방식)
    public async Task<Dictionary<string, string[]>> ChangeAsync(ChangePasswordRequest request)
    {
        if (request.NewPassword == request.CurrentPassword)
            return new() { [nameof(request.NewPassword)] = ["현재 비밀번호와 다른 비밀번호를 입력하세요."] };

        var user = await userManager.FindByIdAsync(currentUser.Id ?? throw new InvalidOperationException("로그인한 사용자가 없습니다."))
            ?? throw new InvalidOperationException("로그인한 사용자를 찾을 수 없습니다.");

        // 현재 비밀번호 확인 → 새 비밀번호 규칙 검사 → Hash로 저장. 셋 다 Identity가 한다.
        // 성공하면 SecurityStamp(사용자별 "보안 버전" 값)도 새로 바뀐다.
        //   → 다른 기기에 남은 예전 Cookie는 SecurityStamp가 달라서 다음 확인 때 로그아웃된다 (Program.cs의 ValidationInterval)
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return result.Errors
                .Select(ToFieldError)
                .GroupBy(x => x.Field)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

        // 지금 이 기기는 로그인을 유지한다. 새 SecurityStamp가 담긴 Cookie로 다시 발급한다. ("로그인 유지" 설정은 그대로)
        await signInManager.RefreshSignInAsync(user);
        return [];
    }

    private static (string Field, string Message) ToFieldError(IdentityError e) => e.Code switch
    {
        // 현재 비밀번호가 틀림
        "PasswordMismatch" => (nameof(ChangePasswordRequest.CurrentPassword), "현재 비밀번호가 올바르지 않습니다."),
        _ when PasswordRules.Message(e) is { } message => (nameof(ChangePasswordRequest.NewPassword), message),
        _ => (string.Empty, e.Description),
    };
}
