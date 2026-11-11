using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// 저금 하나 응답 (목록·등록·수정). 값이 null인 속성은 JSON에서 빠진다 (Program.cs 설정)
// Balance: 현재 금액 = 기존 금액 + 입금 + 이자 − 출금. 저장된 값이 아니라 기록에서 그때그때 계산한다 (Saving 설명 참고)
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
    decimal Balance);

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
