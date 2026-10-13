import { queryClient } from '@/lib/queryClient'
import { ApiError, apiRequest } from '@/services/api'
import type { LoginInput, Me, RegisterInput } from '@/types/auth'

// 회원가입. 실패하면 ApiError (fieldErrors에 입력칸별 오류). 가입 후 자동 로그인하지 않는다.
export async function register(input: RegisterInput): Promise<void> {
  await apiRequest<void>('/api/auth/register', { method: 'POST', body: input })
}

// 로그인. 성공하면 Backend가 인증 Cookie(HttpOnly)를 저장시킨다. 화면 코드는 Cookie를 직접 다루지 않는다.
export async function login(input: LoginInput): Promise<void> {
  await apiRequest<void>('/api/auth/login', { method: 'POST', body: input })
  // 다른 사람이 쓰던 데이터가 캐시에 남아 보이지 않도록 비운다.
  queryClient.clear()
}

export async function logout(): Promise<void> {
  try {
    await apiRequest<void>('/api/auth/logout', { method: 'POST' })
  } finally {
    // 로그아웃하면 TanStack Query 캐시에 남은 내 데이터를 모두 지운다.
    // 같은 브라우저에서 다른 사람이 로그인했을 때 이전 사람의 데이터가 잠깐이라도 보이면 안 된다.
    queryClient.clear()
  }
}

// 현재 로그인한 사용자. 로그인하지 않았으면(401) null.
// HttpOnly Cookie는 JavaScript로 읽을 수 없으므로, 로그인 여부는 이렇게 서버에 물어서 확인한다.
export async function getMe(): Promise<Me | null> {
  try {
    return await apiRequest<Me>('/api/me')
  } catch (error) {
    // instanceof: error가 ApiError로 만들어진 값인지 확인한다. 401이 아닌 오류(서버 꺼짐 등)는 그대로 다시 던진다.
    if (error instanceof ApiError && error.status === 401) return null
    throw error
  }
}
