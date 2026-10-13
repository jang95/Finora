namespace Finora.Api.Data.Entities;

// enum = 정해진 값 중 하나만 갖는 타입. DB에는 "Income"/"Expense" 문자열로 저장한다. (FinoraDbContext 참고)
// Frontend의 CategoryType('Income' | 'Expense')과 같은 값이다.
public enum CategoryType
{
    Income,
    Expense,
}

// 수입/지출 카테고리 (계획서 8장). 사용자 소유 테이블의 첫 번째 예. (결정 D15)
// 사용자 소유 테이블은 모두 UserId를 갖고, FinoraDbContext의 Global Query Filter로 현재 사용자 것만 조회된다.
public class Category
{
    public int Id { get; set; }

    // 소유자 (AspNetUsers.Id). 항상 서버가 인증 정보로 채운다.
    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public CategoryType Type { get; set; }

    // 목록에 보여 줄 순서 (작은 값이 먼저)
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
