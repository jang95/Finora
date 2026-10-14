namespace Finora.Api.DTOs;

// 통계·대시보드 응답들. 합계는 모두 Backend decimal로 계산한다 (결정 D4). 이체는 합계에 넣지 않는다.

// GET /api/statistics/summary — 기간(From~To, 양 끝 포함)의 수입·지출·합계
// + To 날짜까지의 누적 총자산(가계부 기준 = 지금까지 수입 − 지출, 결정 D17)
public record PeriodSummaryResponse(DateOnly From, DateOnly To, decimal Income, decimal Expense, decimal Balance, decimal TotalAssets);

// GET /api/dashboard — Frontend types/dashboard.ts의 DashboardSummary와 같은 모양.
// 기간과 상관없는 대시보드 정보만 담는다. 기간별 수입·지출·총자산은 GET /api/statistics/summary를 쓴다.
// RecentTransactions: 최근 거래 몇 건 (최신 날짜가 위, 같은 날은 나중에 등록한 것이 위)
// 계좌 기준 총자산은 Phase 8에서 추가한다. (결정 D17)
public record DashboardResponse(List<TransactionResponse> RecentTransactions);
