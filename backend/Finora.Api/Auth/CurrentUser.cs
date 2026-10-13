using System.Security.Claims;

namespace Finora.Api.Auth;

// 현재 요청을 보낸 사용자. 값은 항상 인증 Cookie에서 꺼낸다. (요청 Body/URL의 UserId는 믿지 않는다 — 계획서 9장)
// IHttpContextAccessor = Controller 밖(DbContext, Service)에서도 지금 처리 중인 요청 정보를 볼 수 있게 해 주는 도구.
public class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    // 로그인한 사용자의 Id (AspNetUsers.Id). 로그인 전이거나 요청 밖(명령어 실행 등)이면 null.
    // ClaimTypes.NameIdentifier = Identity가 Cookie에 넣어 두는 사용자 Id 항목.
    public string? Id => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
