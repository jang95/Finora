using Finora.Api.Data.Entities;
using Finora.Api.Services;

namespace Finora.Api.DTOs;

// 저금 하나 응답 (목록·등록·수정). 값이 null인 속성은 JSON에서 빠진다 (Program.cs 설정)
// Balance: 현재 금액 = 기존 금액 + 입금 + 이자 − 출금. 저장된 값이 아니라 기록에서 그때그때 계산한다 (Saving 설명 참고)
// Calculation: 예상 이자·세후 수령액·진행률·계획 대비 (SavingCalculation 설명 참고). JSON에서는 "calculation": { … } 안쪽 객체가 된다
public record SavingResponse(
    int Id,
    string Name,
    SavingKind Kind,
    string? BankName,
    decimal? TargetAmount,
    DateOnly StartDate,
    DateOnly? MaturityDate,
    decimal? InterestRate,
    InterestMethod InterestMethod,
    decimal TaxRate,
    SavingPaymentType? PaymentType,
    decimal? PlannedAmount,
    SavingStatus Status,
    string? Memo,
    decimal Balance,
    SavingCalculation Calculation);

// 저금 기록 한 건. 이자를 가계부 수입으로도 기록했으면 그 거래의 Id·카테고리가 함께 온다
public record SavingEntryResponse(
    int Id,
    DateOnly Date,
    SavingEntryType Type,
    decimal Amount,
    string? Memo,
    int? TransactionId,
    int? CategoryId,
    string? CategoryName);

// GET /api/savings/{id} — 저금 + 기록 목록(최신 날짜순)
public record SavingDetailResponse(SavingResponse Saving, List<SavingEntryResponse> Entries);

// GET /api/savings/summary?from&to — 기간별 저금 합계 (가계부 요약·대시보드 저금 카드, 결정 D24)
// - Deposited / Withdrawn / Interest: 기간 안의 입금·출금·이자 합계. 기존 금액(Opening)은 뺀다 (지난 기간에 모은 돈)
// - NetSaved: 이번 기간 저금 = 입금 + 이자 − 출금. "저금 후 남은 돈" = 가계부 합계 − NetSaved
// - TotalBalance: to 날짜까지의 모든 저금 현재 금액 합계 (기존 금액 포함)
// - ProtectionWarnings: 금융기관별 현재 금액 합계가 예금자보호 한도(1억 원)를 넘는 곳 (금융기관 이름을 적은 저금만)
public record SavingsSummaryResponse(
    DateOnly From,
    DateOnly To,
    decimal Deposited,
    decimal Withdrawn,
    decimal Interest,
    decimal NetSaved,
    decimal TotalBalance,
    List<BankTotalResponse> ProtectionWarnings);

public record BankTotalResponse(string BankName, decimal Balance);
