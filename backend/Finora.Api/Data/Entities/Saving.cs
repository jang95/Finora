namespace Finora.Api.Data.Entities;

// 저금 종류 (결정 D24). DB와 JSON에는 "Free"/"Installment"/"Deposit" 문자열로 주고받는다.
// - Free: 자유저금. 목표 금액·목표일(선택)을 정하고 자유롭게 넣고 뺀다
// - Installment: 적금. 가입일~만기일 동안 나눠 넣는다. 출금 없이 만기 수령·중도해지만
// - Deposit: 예금. 원금을 한 번 넣고 만기까지 둔다
public enum SavingKind
{
    Free,
    Installment,
    Deposit,
}

// 이자 계산 방식. Simple = 단리(원금에만 이자), MonthlyCompound = 월복리(매달 붙은 이자에도 이자)
public enum InterestMethod
{
    Simple,
    MonthlyCompound,
}

// 적금 납입 방식. Fixed = 매달 같은 금액(정액), Flexible = 자유적립, WeeklyIncreasing = 매주 첫 주 금액만큼 늘려 넣기(26주형)
public enum SavingPaymentType
{
    Fixed,
    Flexible,
    WeeklyIncreasing,
}

// 저금 상태. Active = 진행 중, Completed = 달성·만기 수령·해지, Archived = 보관 (목록에서 숨기되 기록은 남김)
public enum SavingStatus
{
    Active,
    Completed,
    Archived,
}

// 저금 하나 (계획서 8장 Savings, 결정 D24). 사용자 소유 테이블 → Global Query Filter 적용 (FinoraDbContext)
// 가계부 거래(Transaction)와는 별개다. 넣고 빼는 돈은 SavingEntry로 기록하고, 현재 금액은 기록의 합으로 계산한다.
// (현재 금액을 컬럼으로 저장하지 않는 이유: 기록과 저장된 합계가 어긋날 일이 없다)
public class Saving
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public SavingKind Kind { get; set; }

    // 금융기관 이름 (선택). 기관별 합계로 예금자보호 한도(1억 원) 안내에 쓴다
    public string? BankName { get; set; }

    // 자유저금의 목표 금액. 적금·예금은 납입 일정·원금으로 계산하므로 비워 둔다
    public decimal? TargetAmount { get; set; }

    // 시작일(적금·예금은 가입일). 계획 대비 진행과 이자 계산의 기준
    public DateOnly StartDate { get; set; }

    // 자유저금은 목표일(선택), 적금·예금은 만기일(필수). 필수 여부는 SavingService가 종류별로 확인한다
    public DateOnly? MaturityDate { get; set; }

    // 연이율(%). 3.5% → 3.5. 자유저금은 선택
    public decimal? InterestRate { get; set; }

    public InterestMethod InterestMethod { get; set; }

    // 이자에 붙는 세율(%). 일반과세 15.4 = 소득세 14 + 지방소득세 1.4, 비과세 0
    public decimal TaxRate { get; set; }

    // 적금만 쓴다. 자유저금·예금은 null
    public SavingPaymentType? PaymentType { get; set; }

    // 계획 금액: 정액 적금의 월 납입액, 26주형의 첫 주 금액, 자유저금의 월 계획 금액
    public decimal? PlannedAmount { get; set; }

    public SavingStatus Status { get; set; }

    public string? Memo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
