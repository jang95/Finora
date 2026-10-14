namespace Finora.Api.DTOs;

// 통계·대시보드 응답들. 합계는 모두 Backend decimal로 계산한다 (결정 D4). 이체는 합계에 넣지 않는다.

// GET /api/statistics/summary — 기간(From~To, 양 끝 포함)의 수입·지출·합계
// + To 날짜까지의 누적 총자산(가계부 기준 = 지금까지 수입 − 지출, 결정 D17)
public record PeriodSummaryResponse(DateOnly From, DateOnly To, decimal Income, decimal Expense, decimal Balance, decimal TotalAssets);
