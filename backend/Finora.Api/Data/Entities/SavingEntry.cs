namespace Finora.Api.Data.Entities;

// 저금 기록 종류 (결정 D24)
// - Opening: 기존 금액. 이미 가입해 진행 중인 상품을 등록할 때 지금까지 넣은 돈. 저금 하나에 1건만,
//   현재 금액에는 포함하지만 "이번 달 저금" 같은 기간 합계에서는 뺀다 (이미 지난 기간에 모은 돈이므로)
// - Deposit / Withdrawal: 입금 / 출금
// - Interest: 실제로 받은 이자 (세후). 가계부 수입과 연결할 수 있다
public enum SavingEntryType
{
    Opening,
    Deposit,
    Withdrawal,
    Interest,
}

// 저금에 넣고 뺀 기록 한 건 (계획서 8장 SavingEntries). 사용자 소유 테이블 → Global Query Filter 적용
// 입금·출금은 가계부 수입·지출이 아니다. 이미 가진 돈을 옮기는 것이라 통계와 가계부 기준 총자산에 영향이 없다. (결정 D24)
public class SavingEntry
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int SavingId { get; set; }

    // 탐색 속성 (Transaction.Category 설명 참고)
    public Saving Saving { get; set; } = null!;

    public DateOnly Date { get; set; }

    public SavingEntryType Type { get; set; }

    // 항상 양수. 들어온 돈인지 나간 돈인지는 Type으로 구분한다 (Transaction.Amount와 같은 규칙)
    public decimal Amount { get; set; }

    public string? Memo { get; set; }

    // 이자를 가계부 수입으로 함께 기록했을 때 그 거래. 이자 기록을 고치거나 지우면 거래도 같이 바꾼다
    public int? TransactionId { get; set; }

    public Transaction? Transaction { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
