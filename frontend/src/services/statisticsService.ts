import { transactionsMock } from '@/mocks/transactions'
import type { CategoryExpense, MonthlyTotal } from '@/types/statistics'
import { addMonths, type YearMonth } from '@/utils/yearMonth'

// 지금은 Backend 대신 여기서 mock 거래내역으로 합계를 낸다. 화면(pages)에서는 금액을 더하지 않는다 (결정 D4)
// 가계부에서 거래를 추가·수정하면 통계에도 반영된다 (새로고침하면 초기화)

// 기준 월까지 최근 count개월의 수입/지출 합계 (오래된 달이 앞).
// Backend가 생기면 GET /api/statistics/monthly?year=&month=&count= 호출로 바꾼다.
export async function getMonthlyTotals(end: YearMonth, count: number): Promise<MonthlyTotal[]> {
  // Array.from({ length: n }, (_, i) => ...): 0 ~ n-1 번호로 길이 n짜리 배열을 만든다. C#의 Enumerable.Range와 비슷하다.
  //   _ 는 "이 매개변수는 안 쓴다"는 관례적인 이름
  return Array.from({ length: count }, (_, i) => {
    const { year, month } = addMonths(end, i - (count - 1))
    const items = getMonthItems(year, month)
    return {
      year,
      month,
      income: sumAmounts(items.filter((t) => t.type === 'Income')),
      expense: sumAmounts(items.filter((t) => t.type === 'Expense')),
    }
  })
}

// 해당 월의 카테고리별 지출 (금액 큰 순). Backend가 생기면 GET /api/statistics/categories?year=&month= 호출로 바꾼다.
export async function getCategoryExpenses(year: number, month: number): Promise<CategoryExpense[]> {
  const expenses = getMonthItems(year, month).filter((t) => t.type === 'Expense')
  const total = sumAmounts(expenses)

  // Map<키, 값>: 키로 값을 찾는 자료구조 (C#의 Dictionary). 카테고리 id별로 금액을 모은다.
  const byCategory = new Map<number, CategoryExpense>()
  for (const t of expenses) {
    // 지출은 항상 카테고리가 있다. 혹시 없으면 0(미분류)으로 모은다
    const categoryId = t.categoryId ?? 0
    const current = byCategory.get(categoryId)
    if (current) {
      current.amount += t.amount
    } else {
      byCategory.set(categoryId, { categoryId, categoryName: t.categoryName ?? '미분류', amount: t.amount, ratio: 0 })
    }
  }

  // [...map.values()]: Map의 값들을 배열로 펼친다
  return [...byCategory.values()]
    .map((c) => ({ ...c, ratio: total === 0 ? 0 : (c.amount / total) * 100 }))
    .sort((a, b) => b.amount - a.amount)
}

function getMonthItems(year: number, month: number) {
  const prefix = `${year}-${String(month).padStart(2, '0')}-`
  return transactionsMock.filter((t) => t.date.startsWith(prefix))
}

function sumAmounts(items: { amount: number }[]): number {
  return items.reduce((sum, t) => sum + t.amount, 0)
}
