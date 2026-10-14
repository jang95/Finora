namespace Finora.Api.Data.Entities;

// 거래 종류. DB와 JSON에는 "Income"/"Expense"/"Transfer" 문자열로 주고받는다.
// Transfer(계좌 간 이체)는 계좌 기능(Phase 8)이 생기기 전까지 저장을 거절한다. (결정 D16)
public enum TransactionType
{
    Income,
    Expense,
    Transfer,
}

// 가계부 거래 한 건 (계획서 8장 Transactions). 사용자 소유 테이블 → Global Query Filter 적용 (FinoraDbContext)
// 계좌(AccountId)·받는 계좌(ToAccountId)는 Phase 8에서 선택 컬럼으로 추가한다. 그전 거래는 "계좌 미지정"으로 남는다. (결정 D16)
public class Transaction
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    // 거래일. 시간 없이 날짜만 (한국 기준 날짜) → PostgreSQL date. DateOnly = 날짜만 담는 .NET 타입
    public DateOnly Date { get; set; }

    public TransactionType Type { get; set; }

    // 수입/지출은 카테고리가 있다. (이체는 없음 — 그래서 null 허용)
    public int? CategoryId { get; set; }

    // 탐색 속성(navigation): 조회할 때 t.Category.Name처럼 연결된 카테고리를 따라갈 수 있다. EF Core가 JOIN으로 바꿔 준다.
    public Category? Category { get; set; }

    // 금액은 항상 양수. 수입/지출 구분은 Type으로 한다. decimal → PostgreSQL numeric(18,2) (결정 D4, 계획서 8장)
    public decimal Amount { get; set; }

    public string? Memo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
