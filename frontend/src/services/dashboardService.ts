import { apiRequest } from '@/services/api'
import { transactionsQueryKey } from '@/services/transactionService'
import type { DashboardSummary } from '@/types/dashboard'

// 대시보드 API (Phase 6). 총자산·이번 달 합계는 Backend가 decimal로 계산하고,
// "이번 달"도 Backend가 한국 시간으로 정한다 (결정 D4, D18). 화면은 그리기만 한다.

// 캐시 이름표를 거래 이름표(['transactions']) 아래에 둔다.
// 대시보드 숫자는 거래로 만든 값이라, 거래를 등록·수정·삭제하면 함께 낡아야 한다.
// 가계부·설정 화면이 이미 ['transactions']를 invalidate하므로, 그때 대시보드도 자동으로 다시 불러와진다.
export const dashboardQueryKey = [...transactionsQueryKey, 'dashboard'] as const

export async function getDashboardSummary(): Promise<DashboardSummary> {
  return apiRequest<DashboardSummary>('/api/dashboard')
}
