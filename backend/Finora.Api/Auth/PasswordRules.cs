using Microsoft.AspNetCore.Identity;

namespace Finora.Api.Auth;

// 비밀번호 규칙 위반 문구. 회원가입(RegistrationService)과 비밀번호 변경(PasswordService)이 같이 쓴다.
// 규칙 자체(8자 이상, 숫자 포함)는 Program.cs의 Identity 설정에 있다.
public static class PasswordRules
{
    // Identity 오류가 비밀번호 규칙 위반이면 한국어 문구, 아니면 null
    public static string? Message(IdentityError e) => e.Code switch
    {
        "PasswordTooShort" => "비밀번호는 8자 이상이어야 합니다.",
        "PasswordRequiresDigit" => "비밀번호에 숫자를 1개 이상 넣으세요.",
        // 그 밖의 비밀번호 규칙 (지금 설정에서는 나오지 않지만, 규칙을 바꾸면 Identity 기본 문구로 보여준다)
        _ when e.Code.StartsWith("Password") => e.Description,
        _ => null,
    };
}
