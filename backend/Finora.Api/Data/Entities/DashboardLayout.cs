namespace Finora.Api.Data.Entities;

// 화면의 위젯 배치 (계획서 8장 DashboardLayouts, 결정 D27). 사용자 소유 테이블 → Global Query Filter 적용 (FinoraDbContext)
// 사용자·화면(Page)당 1행. 행이 없으면 Frontend가 기본 배치를 쓴다.
public class DashboardLayout
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    // 어느 화면의 배치인지. 지금은 "statistics"만. 나중에 대시보드 화면도 같은 테이블을 쓴다
    public string Page { get; set; } = null!;

    // 배치 전체를 JSON 문자열 하나로 저장한다: [{ "id": "summary", "size": "full", "hidden": false }, ...] (순서가 화면 순서)
    // 위젯마다 행을 두지 않는 이유는 결정 D27 참고. DB 컬럼 타입은 jsonb (FinoraDbContext) — 문자열이지만 PostgreSQL이 JSON인지 검사한다.
    public string LayoutJson { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
