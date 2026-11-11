using System.ComponentModel.DataAnnotations;
using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// 저금 API 요청들 (결정 D24). 값 타입을 "?"(null 허용)로 둔 이유는 CreateCategoryRequest 설명 참고.
// 종류별로 필요한 칸이 달라서(예: 예금은 만기일 필수, 자유저금은 선택) [Required]로 다 잡을 수 없다. 나머지는 SavingService가 확인한다.

// POST /api/savings, PUT /api/savings/{id} 요청 (등록과 수정이 같은 모양).
// 종류에 맞지 않는 칸(예: 예금의 PaymentType)은 보내도 저장하지 않는다(null).
// OpeningAmount: 이미 가입해 진행 중인 상품의 기존 금액. 있으면 기록 1건(Opening, 가입일 날짜)으로 저장한다.
//   수정할 때도 이 값으로 맞춘다 — 비우거나 0이면 기존 금액 기록을 지운다. (PUT은 "전체를 이 값으로 바꾸기")
public record SaveSavingRequest(
    [Required(ErrorMessage = "저금 이름을 입력하세요.")]
    [MaxLength(50, ErrorMessage = "저금 이름은 50자 이하로 입력하세요.")]
    string Name,

    [Required(ErrorMessage = "저금 종류를 선택하세요.")]
    SavingKind? Kind,

    [MaxLength(50, ErrorMessage = "금융기관 이름은 50자 이하로 입력하세요.")]
    string? BankName,

    decimal? TargetAmount,

    [Required(ErrorMessage = "시작일(가입일)을 입력하세요.")]
    DateOnly? StartDate,

    DateOnly? MaturityDate,

    decimal? InterestRate,

    InterestMethod? InterestMethod,

    decimal? TaxRate,

    SavingPaymentType? PaymentType,

    decimal? PlannedAmount,

    [MaxLength(100, ErrorMessage = "메모는 100자 이하로 입력하세요.")]
    string? Memo,

    decimal? OpeningAmount);

// PUT /api/savings/{id}/status 요청 — 진행 중 / 완료(달성) / 보관
public record UpdateSavingStatusRequest(
    [Required(ErrorMessage = "상태를 선택하세요.")]
    SavingStatus? Status);

// POST /api/savings/{id}/entries, PUT /api/savings/{id}/entries/{entryId} 요청.
// Type은 Deposit / Withdrawal / Interest만 받는다. (기존 금액 Opening은 저금 등록·수정의 OpeningAmount로)
// RecordAsIncome, CategoryId는 이자(Interest)일 때만 쓴다:
//   RecordAsIncome — 가계부 수입으로도 기록할지 (비우면 켬, 결정 D24)
//   CategoryId — 그 수입의 카테고리. 비우면 내 "이자" 수입 카테고리
public record SaveSavingEntryRequest(
    [Required(ErrorMessage = "날짜를 입력하세요.")]
    DateOnly? Date,

    [Required(ErrorMessage = "기록 종류를 선택하세요.")]
    SavingEntryType? Type,

    [Required(ErrorMessage = "금액을 입력하세요.")]
    decimal? Amount,

    [MaxLength(100, ErrorMessage = "메모는 100자 이하로 입력하세요.")]
    string? Memo,

    bool? RecordAsIncome,

    int? CategoryId);

// 적금·예금을 끝내는 방법. Maturity = 만기 수령, Cancel = 중도해지
// (DB에 따로 저장하지 않는다. 만들어지는 출금 기록의 메모로 남는다)
public enum SavingCloseType
{
    Maturity,
    Cancel,
}

// POST /api/savings/{id}/close 요청 — 만기 수령·중도해지.
// Interest: 실제로 받은 이자(세후). 있으면 이자 기록(+ 가계부 수입)을 만들고, 그다음 남은 금액 전부를 출금으로 기록한다.
public record CloseSavingRequest(
    [Required(ErrorMessage = "만기 수령/중도해지를 선택하세요.")]
    SavingCloseType? Type,

    [Required(ErrorMessage = "날짜를 입력하세요.")]
    DateOnly? Date,

    decimal? Interest,

    bool? RecordInterestAsIncome,

    int? CategoryId);
