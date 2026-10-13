namespace Finora.Api.Data.Entities;

// Entity = DB 테이블 한 줄(row)을 나타내는 C# 클래스. EF Core가 이 클래스를 보고 테이블을 만든다.
// 초대코드는 사용자 공용 테이블이라 UserId(소유자)가 없다. (계획서 8장)
public class InviteCode
{
    // 이름이 Id인 속성은 EF Core가 자동으로 기본 키(Primary Key)로 정한다.
    public int Id { get; set; }

    // "= null!"은 "EF Core가 값을 채워 주니 null 경고를 내지 말라"는 표시다.
    public string Code { get; set; } = null!;

    // 시각은 UTC로 저장한다 → PostgreSQL timestamptz
    public DateTime CreatedAt { get; set; }

    // "?"는 null 허용. 아직 사용하지 않은 코드는 값이 없다.
    public DateTime? UsedAt { get; set; }

    // 이 코드로 가입한 사용자 Id (AspNetUsers.Id 외래 키, FinoraDbContext 참고)
    public string? UsedByUserId { get; set; }
}
