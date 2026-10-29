import { apiRequest } from '@/services/api'
import type { Category, NewCategory } from '@/types/category'

// 카테고리 API (Phase 5). 규칙 검사(같은 부모 아래 이름 중복, 최대 3단계, 내 카테고리인지)는 Backend가 한다.
// 실패하면 ApiError를 던지고, message에 화면에 보여줄 문구가 들어 있다.

// TanStack Query가 이 데이터를 캐시에 보관할 때 쓰는 이름표(query key).
// 카테고리를 바꾼 뒤 이 이름표로 invalidate하면, 카테고리를 쓰는 화면이 모두 다시 불러온다.
export const categoriesQueryKey = ['categories'] as const

export async function getCategories(): Promise<Category[]> {
  return apiRequest<Category[]>('/api/categories')
}

export async function addCategory(input: NewCategory): Promise<Category> {
  return apiRequest<Category>('/api/categories', { method: 'POST', body: input })
}

// 이름 변경. 종류(수입/지출)는 바꾸지 않는다 — 이미 쓴 거래의 의미가 달라지기 때문.
export async function renameCategory(id: number, name: string): Promise<Category> {
  return apiRequest<Category>(`/api/categories/${id}`, { method: 'PUT', body: { name } })
}

// 삭제. 거래에서 쓰고 있거나 하위 분류가 있는 카테고리는 Backend가 거절한다(409).
export async function deleteCategory(id: number): Promise<void> {
  await apiRequest<void>(`/api/categories/${id}`, { method: 'DELETE' })
}
