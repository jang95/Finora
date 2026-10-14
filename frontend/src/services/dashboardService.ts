import { transactionsMock } from '@/mocks/transactions'
import { getAssetSummary } from '@/services/assetService'
import { getMonthlyTotals } from '@/services/statisticsService'
import type { DashboardSummary } from '@/types/dashboard'
import type { Transaction } from '@/types/transaction'
import { getThisMonth } from '@/utils/yearMonth'

// 대시보드에 보여줄 최근 거래 수
const RECENT_COUNT = 5

// 화면은 이 함수만 호출한다. 지금은 다른 service의 mock 계산을 모아서 돌려주고,
// Phase 6에서 GET /api/dashboard 호출 한 번으로 바꾼다. 화면 코드는 그대로 둔다. (결정 D5)
// 실제 API 호출은 시간이 걸리므로 처음부터 Promise(나중에 도착하는 값)를 반환하는 async 함수로 만든다.
export async function getDashboardSummary(): Promise<DashboardSummary> {
  const [assets, [thisMonth]] = await Promise.all([
    getAssetSummary(),
    // 이번 달 1개월치만. 결과는 배열이라 [thisMonth]로 첫 항목을 꺼낸다 (배열 구조 분해)
    getMonthlyTotals(getThisMonth(), 1),
  ])

  return {
    totalAssets: assets.totalBalance,
    monthlyIncome: thisMonth.income,
    monthlyExpense: thisMonth.expense,
    recentTransactions: getRecentMockTransactions(RECENT_COUNT),
  }
}

// 최근 거래 count건 (최신이 위). Phase 6에서 /api/dashboard 응답으로 대체한다.
// [...배열]로 복사한 뒤 정렬한다. sort는 원본 배열 순서를 바꿔버리기 때문 (mock 원본은 그대로 둔다)
// 정렬 기준: 'YYYY-MM-DD' 문자열은 사전순 = 날짜순이라 문자열 비교로 정렬할 수 있다. 같은 날은 id 큰 것이 위.
function getRecentMockTransactions(count: number): Transaction[] {
  return [...transactionsMock].sort((a, b) => b.date.localeCompare(a.date) || b.id - a.id).slice(0, count)
}
