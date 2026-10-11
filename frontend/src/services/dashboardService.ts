import { getAssetSummary } from '@/services/assetService'
import { getMonthlyTotals } from '@/services/statisticsService'
import { getRecentTransactions } from '@/services/transactionService'
import type { DashboardSummary } from '@/types/dashboard'
import { getThisMonth } from '@/utils/yearMonth'

// 대시보드에 보여줄 최근 거래 수
const RECENT_COUNT = 5

// 화면은 이 함수만 호출한다. 지금은 다른 service의 mock 계산을 모아서 돌려주고,
// Backend가 생기면 GET /api/dashboard 호출 한 번으로 바꾼다. 화면 코드는 그대로 둔다. (결정 D5)
// 실제 API 호출은 시간이 걸리므로 처음부터 Promise(나중에 도착하는 값)를 반환하는 async 함수로 만든다.
export async function getDashboardSummary(): Promise<DashboardSummary> {
  const [assets, [thisMonth], recentTransactions] = await Promise.all([
    getAssetSummary(),
    // 이번 달 1개월치만. 결과는 배열이라 [thisMonth]로 첫 항목을 꺼낸다 (배열 구조 분해)
    getMonthlyTotals(getThisMonth(), 1),
    getRecentTransactions(RECENT_COUNT),
  ])

  return {
    totalAssets: assets.totalBalance,
    monthlyIncome: thisMonth.income,
    monthlyExpense: thisMonth.expense,
    recentTransactions,
  }
}
