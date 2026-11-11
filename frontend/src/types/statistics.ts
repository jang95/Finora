// 통계 API 응답 (Backend DTOs/StatisticsResponses.cs와 같은 모양). 합계·비율은 Backend가 decimal로 계산해서 내려준다 (결정 D4)

// 한 달의 수입/지출 합계 (이체는 포함하지 않음)
export interface MonthlyTotal {
  year: number
  month: number // 1~12
  income: number
  expense: number
  // 저축률(%) = (수입 − 지출) ÷ 수입. 수입이 0인 달은 null (Backend가 계산, 결정 D4)
  savingRate: number | null
  // 그달 말 기준 누적 총자산 (가계부 기준, 결정 D17)
  totalAssets: number
}

// 기간 지출의 대분류 상위 N개 + "기타" 비중 (GET /api/statistics/categories/top). categoryId가 null이면 "기타" 묶음
export interface CategoryShares {
  total: number
  items: CategoryShare[]
}

export interface CategoryShare {
  categoryId: number | null
  name: string
  amount: number
  ratio: number // %
}

// 달마다 대분류 상위 N개 + "기타"의 지출 (GET /api/statistics/categories/monthly).
// months[m].amounts[i]가 series[i]의 그달 지출. 순서가 같아서 i로 맞춘다
export interface CategoryMonthly {
  series: CategorySeries[]
  months: CategoryMonthRow[]
}

export interface CategorySeries {
  categoryId: number | null
  name: string
}

export interface CategoryMonthRow {
  year: number
  month: number
  amounts: number[]
}

// 요일별 평균 지출 (GET /api/statistics/weekdays). 월~일 7개. 평균은 Backend가 "합계 ÷ 기간 안 그 요일 날 수"로 계산 (결정 D4)
export type Weekday = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'

export interface WeekdayAverage {
  weekday: Weekday
  dayCount: number
  total: number
  average: number
}

// 하루의 수입/지출 합계와 기간 첫날부터의 지출 누적 (GET /api/statistics/daily). 누적도 Backend가 계산한다 (결정 D4)
export interface DailyTotal {
  date: string // 'YYYY-MM-DD'
  income: number
  expense: number
  cumulativeExpense: number
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
