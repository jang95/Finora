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

    // 상위 카테고리 Id. 대분류는 null, 중분류는 대분류 Id, 소분류는 중분류 Id (최대 3단계, 결정 D23)
    // 같은 Categories 테이블의 다른 행을 가리킨다 = 자기 참조(self-reference). 단계마다 테이블을 따로 만들지 않는다.
    public int? ParentId { get; set; }

    // 탐색 속성: c.Parent.Name처럼 상위 카테고리를 따라갈 수 있다.
    public Category? Parent { get; set; }

    public string Name { get; set; } = null!;

    // 하위 분류는 항상 대분류와 같은 종류다. (서버가 대분류에서 복사해 채운다)
    public CategoryType Type { get; set; }

    // 목록에 보여 줄 순서 (작은 값이 먼저)
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
