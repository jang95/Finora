namespace Finora.Api.DTOs;

// GET /api/me 응답. 화면에 필요한 값만 담는다. (FinoraUser를 그대로 내보내면 PasswordHash 등이 섞여 나간다)
public record MeResponse(string Email);
