import { apiRequest } from '@/services/api'
import { transactionsQueryKey } from '@/services/transactionService'
import type { CategoryExpense, MonthlyTotal, PeriodSummary } from '@/types/statistics'

// 통계 API (Phase 6). 합계·비율은 Backend가 decimal로 계산해서 내려준다 (결정 D4). 화면은 그리기만 한다.
// 기간은 모두 from ~ to ('YYYY-MM-DD', 양 끝 포함)로 보낸다. (utils/period.ts의 Period)

// 캐시 이름표. 거래로 만든 값이라 dashboardQueryKey처럼 ['transactions'] 아래에 둔다 (거래를 바꾸면 함께 다시 불러옴)
// from, to가 이름표에 들어 있어서 기간을 바꾸면 그 기간을 새로 불러온다.
export const periodSummaryQueryKey = (from: string, to: string) => [...transactionsQueryKey, 'summary', from, to] as const

// parentId(펼친 카테고리)도 이름표에 넣는다. 단계마다 따로 캐시되어 되돌아갈 때 다시 불러오지 않는다. 대분류 단계는 null
export const categoryExpensesQueryKey = (from: string, to: string, parentId: number | null) =>
  [...transactionsQueryKey, 'categories', from, to, parentId] as const

export const monthlyTotalsQueryKey = (from: string, to: string) => [...transactionsQueryKey, 'monthlyTotals', from, to] as const

// 기간 수입·지출·합계 + to 기준 누적 총자산
export async function getPeriodSummary(from: string, to: string): Promise<PeriodSummary> {
  return apiRequest<PeriodSummary>(`/api/statistics/summary?from=${from}&to=${to}`)
}

// 기간의 카테고리별 지출과 비율 (금액 큰 순). parentId가 null이면 대분류별, 있으면 그 카테고리 바로 아래 단계별
export async function getCategoryExpenses(from: string, to: string, parentId: number | null): Promise<CategoryExpense[]> {
  const parentQuery = parentId === null ? '' : `&parentId=${parentId}`
  return apiRequest<CategoryExpense[]>(`/api/statistics/categories?from=${from}&to=${to}${parentQuery}`)
}

// 기간에 걸친 달마다의 수입/지출 (오래된 달이 앞, 거래 없는 달은 0, 최대 36개월)
export async function getMonthlyTotals(from: string, to: string): Promise<MonthlyTotal[]> {
  return apiRequest<MonthlyTotal[]>(`/api/statistics/monthly?from=${from}&to=${to}`)
}
