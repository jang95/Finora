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

// GET /api/statistics/categories — 기간의 카테고리별 지출 (금액 큰 순). Frontend types/statistics.ts의 CategoryExpense와 같은 모양.
// 한 번에 한 단계만 돌려준다 (결정 D23). parentId가 없으면 대분류별, 있으면 그 카테고리 바로 아래 단계별.
// Amount: 그 카테고리와 모든 하위 분류의 지출 합계 (교통 = 교통 + 대중교통 + 버스 + ...)
// Ratio: 이 단계 합계 중 비율(%). 소수 첫째 자리까지 반올림 (예: 35.48 → 35.5). 반올림 때문에 합이 정확히 100이 아닐 수 있다.
// HasChildren: 하위 분류가 있어서 눌러서 펼칠 수 있는지
// IsDirect: 하위 분류 없이 상위 카테고리(parentId)에 직접 기록한 지출 묶음. 화면에서 "교통 (세부 없음)"으로 보여준다
public record CategoryExpenseResponse(int CategoryId, string CategoryName, decimal Amount, decimal Ratio, bool HasChildren, bool IsDirect);

// GET /api/statistics/monthly — 기간에 걸친 달마다의 수입/지출 (오래된 달이 앞). Frontend의 MonthlyTotal과 같은 모양.
// SavingRate: 저축률(%) = (수입 − 지출) ÷ 수입 × 100, 소수 첫째 자리. 수입이 0이면 null (나눌 수 없음 → 화면은 그달을 비운다). 위젯 ⑩
// TotalAssets: 그달 말 기준 누적 총자산 (가계부 기준 = 처음부터 그달 말까지 수입 − 지출, 결정 D17). 위젯 ⑪
public record MonthlyTotalResponse(int Year, int Month, decimal Income, decimal Expense, decimal? SavingRate, decimal TotalAssets);

// GET /api/statistics/categories/top — 기간 지출을 대분류 상위 N개 + "기타"(나머지 합계)로 나눈 비중 (위젯 ⑥ 도넛).
// Total: 기간 지출 전체. Items의 Ratio는 Total 중 비율(%)이고 합이 반올림 때문에 정확히 100이 아닐 수 있다.
// CategoryId가 null이면 "기타" 묶음 (상위 N개에 못 든 대분류들 + 미분류)
public record CategorySharesResponse(decimal Total, List<CategoryShareResponse> Items);

public record CategoryShareResponse(int? CategoryId, string Name, decimal Amount, decimal Ratio);

// GET /api/statistics/categories/monthly — 기간에 걸친 달마다 대분류 상위 N개 + "기타"의 지출 (위젯 ⑦ 쌓은 막대).
// Series: 선(묶음) 목록. 기간 전체 지출이 큰 순. 마지막이 "기타"(CategoryId null)일 수 있다.
// Months: 달마다 Amounts[i] = Series[i]의 그달 지출 (거래 없는 달은 0). 같은 순서라 화면이 i로 맞춘다
public record CategoryMonthlyResponse(List<CategorySeriesResponse> Series, List<CategoryMonthResponse> Months);

public record CategorySeriesResponse(int? CategoryId, string Name);

public record CategoryMonthResponse(int Year, int Month, List<decimal> Amounts);

// GET /api/statistics/weekdays — 기간 지출의 요일별 평균 (위젯 ⑧). 월~일 순서, 항상 7개.
// Weekday: JSON에는 "Monday"처럼 문자열. DayCount: 기간 안에 그 요일이 몇 번 있는지 (거래 없는 날 포함).
// Average = Total ÷ DayCount, 원 미만 반올림 (DayCount가 0이면 0). 평균도 Backend decimal (결정 D4)
public record WeekdayAverageResponse(DayOfWeek Weekday, int DayCount, decimal Total, decimal Average);

// GET /api/statistics/daily — 기간의 날마다 수입/지출 (거래 없는 날은 0). Frontend의 DailyTotal과 같은 모양.
// CumulativeExpense: 기간 첫날부터 이날까지 지출 누적. 누적도 금액 계산이라 Backend decimal로 한다 (결정 D4) — 화면은 선만 긋는다
public record DailyTotalResponse(DateOnly Date, decimal Income, decimal Expense, decimal CumulativeExpense);
