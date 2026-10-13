using Microsoft.AspNetCore.Identity;

namespace Finora.Api.Data.Entities;

// Finora 사용자. IdentityUser를 상속해서 Id, UserName, PasswordHash, 로그인 실패 횟수·잠금 정보 등을 그대로 쓴다.
// (상속: ": IdentityUser"는 IdentityUser가 가진 속성을 모두 물려받는다는 뜻)
// Id는 문자열(GUID)이다. 다른 테이블의 UserId도 이 값을 가리킨다.
// Finora에만 필요한 사용자 정보가 생기면 여기에 속성을 추가한다.
public class FinoraUser : IdentityUser
{
    public DateTime CreatedAt { get; set; }
}
