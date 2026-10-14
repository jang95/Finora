using System.ComponentModel.DataAnnotations;

namespace Finora.Api.DTOs;

// POST /api/me/password — 비밀번호 변경. 새 비밀번호 규칙(8자 이상, 숫자 포함)은 Identity가 검사한다. (Program.cs)
// 입력칸 오류 키는 속성 이름(CurrentPassword / NewPassword) — Frontend가 입력칸 옆에 보여준다.
public record ChangePasswordRequest(
    [Required(ErrorMessage = "현재 비밀번호를 입력하세요.")]
    [MaxLength(128)]
    string CurrentPassword,

    [Required(ErrorMessage = "새 비밀번호를 입력하세요.")]
    [MaxLength(128)]
    string NewPassword);
