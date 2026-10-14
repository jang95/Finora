namespace Finora.Api.DTOs;

// 가계부 조회 응답들. Frontend types/transaction.ts의 MonthlyLedger / YearlyLedger와 같은 모양이다.
// 합계는 모두 Backend decimal로 계산한다. Frontend는 표시만 한다 (결정 D4). 이체는 합계에 넣지 않는다.

// GET /api/transactions/monthly — 월 요약 + 날짜별 묶음 (최신 날짜가 위). 목록·달력 탭에서 쓴다.
public record MonthlyLedgerResponse(decimal Income, decimal Expense, decimal Balance, List<DailyTransactionsResponse> Days);

// 하루치 거래와 그날 합계 (같은 날 안에서는 나중에 등록한 거래가 위)
public record DailyTransactionsResponse(DateOnly Date, decimal Income, decimal Expense, List<TransactionResponse> Transactions);

// GET /api/transactions/yearly — 한 해 요약 + 1~12월 합계 (월별 탭)
public record YearlyLedgerResponse(decimal Income, decimal Expense, decimal Balance, List<MonthlyBalanceResponse> Months);

public record MonthlyBalanceResponse(int Month, decimal Income, decimal Expense, decimal Balance);
