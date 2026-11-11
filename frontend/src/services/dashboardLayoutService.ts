import { apiRequest } from '@/services/api'
import type { DashboardLayoutResponse, WidgetLayoutItem } from '@/types/dashboardLayout'

// 통계 위젯 배치 API (결정 D27). 검사(아는 위젯인지, 크기 값, 중복)는 Backend가 한다.
// 배치는 거래 데이터와 상관없으므로 ['transactions'] 아래가 아니라 따로 이름표를 둔다 (거래를 바꿔도 다시 불러오지 않음)
export const statisticsLayoutQueryKey = ['dashboardLayout', 'statistics'] as const

// 저장된 배치. 없으면 items가 [] (화면이 기본 배치로 채운다)
export async function getStatisticsLayout(): Promise<DashboardLayoutResponse> {
  return apiRequest<DashboardLayoutResponse>('/api/dashboard-layouts/statistics')
}

// 배치 전체 저장 (덮어쓰기). 같은 내용을 여러 번 보내도 결과가 같아서 PUT (DashboardLayoutsController 설명 참고)
export async function saveStatisticsLayout(items: WidgetLayoutItem[]): Promise<DashboardLayoutResponse> {
  return apiRequest<DashboardLayoutResponse>('/api/dashboard-layouts/statistics', { method: 'PUT', body: { items } })
}
