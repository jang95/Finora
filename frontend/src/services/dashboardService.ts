import { dashboardSummaryMock } from '@/mocks/dashboard'
import type { DashboardSummary } from '@/types/dashboard'

// 화면은 이 함수만 호출한다. 지금은 임시 데이터를 돌려주고,
// Backend가 생기면 함수 안쪽만 fetch로 바꾼다. 화면 코드는 그대로 둔다. (결정 D5)
// 실제 API 호출은 시간이 걸리므로 처음부터 Promise(나중에 도착하는 값)를 반환하는 async 함수로 만든다.
export async function getDashboardSummary(): Promise<DashboardSummary> {
  return dashboardSummaryMock
}
