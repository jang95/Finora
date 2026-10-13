using System.ComponentModel.DataAnnotations;

namespace Finora.Api.DTOs;

// RememberMe = "로그인 유지". true면 브라우저를 닫아도 Cookie가 남는다. (생략하면 false)
public record LoginRequest(
    [Required(ErrorMessage = "이메일을 입력하세요.")]
    [MaxLength(256)]
    string Email,

    [Required(ErrorMessage = "비밀번호를 입력하세요.")]
    [MaxLength(128)]
    string Password,

    bool RememberMe = false);
