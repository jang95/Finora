using System.ComponentModel.DataAnnotations;

namespace Finora.Api.DTOs;

// DTO = API가 주고받는 데이터 모양. Entity(DB 테이블)와 분리해 둔다.
// record = 값을 담기만 하는 간단한 클래스. 생성자 매개변수가 그대로 속성이 된다.
// [Required] 같은 표시(Data Annotations)는 [ApiController]가 자동으로 검사해서, 틀리면 400 응답을 보낸다.
// JSON의 "inviteCode"가 InviteCode 속성으로 들어온다. (대소문자 자동 변환)
public record RegisterRequest(
    [Required(ErrorMessage = "초대코드를 입력하세요.")]
    [MaxLength(32)]
    string InviteCode,

    [Required(ErrorMessage = "이메일을 입력하세요.")]
    [EmailAddress(ErrorMessage = "이메일 형식이 올바르지 않습니다.")]
    [MaxLength(256)]
    string Email,

    // 비밀번호 규칙(8자 이상, 숫자 포함)은 Identity가 검사한다. (Program.cs)
    [Required(ErrorMessage = "비밀번호를 입력하세요.")]
    [MaxLength(128)]
    string Password);
