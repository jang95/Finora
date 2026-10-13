import type { ServerStatus } from '@/types/health'

// Backend 응답 모양. 화면에는 정리된 ServerStatus만 넘긴다.
type HealthResponse = { status: 'ok' | 'degraded'; database: 'connected' | 'unavailable' }

// 첫 실제 API 호출. 주소는 '/api/...'만 쓰고 Backend 주소는 Vite proxy가 붙인다.
export async function getServerStatus(): Promise<ServerStatus> {
  try {
    const response = await fetch('/api/health')
    // fetch는 404·500 같은 오류 응답에서도 예외를 던지지 않는다. 상태 코드를 직접 확인해야 한다.
    if (response.ok) {
      const body = (await response.json()) as HealthResponse
      return body.database === 'connected' ? 'ok' : 'dbDown'
    }
    // 503 = Backend는 살아 있지만 DB 연결 실패. 그 밖의 오류(예: proxy가 Backend를 못 찾은 502/500)는 서버 문제로 본다.
    return response.status === 503 ? 'dbDown' : 'serverDown'
  } catch {
    // 네트워크 자체가 실패하면 fetch가 예외를 던진다.
    return 'serverDown'
  }
}
