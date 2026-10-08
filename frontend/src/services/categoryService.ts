import { categoriesMock } from '@/mocks/categories'
import type { Category } from '@/types/category'

// Backend가 생기면 GET /api/categories 호출로 바꾼다.
export async function getCategories(): Promise<Category[]> {
  return categoriesMock
}
