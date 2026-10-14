// 통계 화면 응답. 합계·비율은 Backend가 decimal로 계산해서 내려준다 (결정 D4)

// 한 달의 수입/지출 합계 (이체는 포함하지 않음)
export interface MonthlyTotal {
  year: number
  month: number // 1~12
  income: number
  expense: number
}

// 한 카테고리의 지출 합계와 그달 전체 지출 중 비율
export interface CategoryExpense {
  categoryId: number
  categoryName: string
  amount: number
  ratio: number // 비율(%) 예: 35.5
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
