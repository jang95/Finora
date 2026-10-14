import { apiRequest } from '@/services/api'
import { transactionsQueryKey } from '@/services/transactionService'
import type { DashboardSummary } from '@/types/dashboard'

// 대시보드 API (Phase 6). 최근 거래를 불러온다.
// 기간별 수입·지출·총자산은 statisticsService의 getPeriodSummary를 쓴다.

// 캐시 이름표를 거래 이름표(['transactions']) 아래에 둔다.
// 최근 거래는 거래 데이터라, 거래를 등록·수정·삭제하면 함께 낡아야 한다.
// 가계부·설정 화면이 이미 ['transactions']를 invalidate하므로, 그때 대시보드도 자동으로 다시 불러와진다.
export const dashboardQueryKey = [...transactionsQueryKey, 'dashboard'] as const

export async function getDashboardSummary(): Promise<DashboardSummary> {
  return apiRequest<DashboardSummary>('/api/dashboard')
}
