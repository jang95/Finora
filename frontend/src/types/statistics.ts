// 통계 API 응답 (Backend DTOs/StatisticsResponses.cs와 같은 모양). 합계·비율은 Backend가 decimal로 계산해서 내려준다 (결정 D4)

// 한 달의 수입/지출 합계 (이체는 포함하지 않음)
export interface MonthlyTotal {
  year: number
  month: number // 1~12
  income: number
  expense: number
}

// 한 카테고리의 지출 합계(하위 분류 포함)와 이 단계 지출 중 비율. 한 번에 한 단계씩 받는다 (결정 D23)
export interface CategoryExpense {
  categoryId: number
  categoryName: string
  amount: number
  ratio: number // 비율(%) 예: 35.5
  hasChildren: boolean // 하위 분류가 있어서 눌러서 펼칠 수 있음
  isDirect: boolean // 하위 분류 없이 상위 카테고리에 직접 기록한 지출 묶음 → "교통 (세부 없음)"
}

// 기간 요약 (GET /api/statistics/summary). from ~ to 양 끝 날짜 포함.
// totalAssets: to 날짜까지의 누적 총자산 (가계부 기준 = 지금까지 수입 − 지출, 결정 D17)
export interface PeriodSummary {
  from: string
  to: string
  income: number
  expense: number
  balance: number
  totalAssets: number
}
