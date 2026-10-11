import { categoriesMock } from '@/mocks/categories'
import { transactionsMock } from '@/mocks/transactions'
import type { Category, NewCategory } from '@/types/category'

// Backend가 생기면 GET /api/categories 호출로 바꾼다.
export async function getCategories(): Promise<Category[]> {
  return categoriesMock
}

// 카테고리 추가. Backend가 생기면 POST /api/categories 호출로 바꾼다.
export async function addCategory(input: NewCategory): Promise<Category> {
  assertUniqueName(input)
  // 자동 증가 id 흉내는 transactionService.addTransaction 설명 참고
  const id = Math.max(0, ...categoriesMock.map((c) => c.id)) + 1
  const created = { ...input, id }
  categoriesMock.push(created)
  return created
}

// 이름 변경. 종류(수입/지출)는 바꾸지 않는다 — 이미 쓴 거래의 의미가 달라지기 때문.
// Backend가 생기면 PUT /api/categories/{id} 호출로 바꾼다.
export async function renameCategory(id: number, name: string): Promise<Category> {
  const category = categoriesMock.find((c) => c.id === id)
  if (category === undefined) throw new Error('카테고리를 찾을 수 없습니다.')
  assertUniqueName({ name, type: category.type }, id)

  category.name = name
  // mock에서는 거래가 카테고리 이름을 복사해 들고 있어서 같이 바꿔준다.
  // (DB에서는 거래가 CategoryId만 가지고 이름은 조인으로 가져오므로 이 작업이 필요 없다)
  for (const t of transactionsMock) {
    if (t.categoryId === id) t.categoryName = name
  }
  return category
}

// 삭제. 거래에서 쓰고 있는 카테고리는 지울 수 없다 (지우면 그 거래들이 "미분류"가 된다).
// Backend가 생기면 DELETE /api/categories/{id} 호출로 바꾸고, 같은 검사를 Backend가 한다.
export async function deleteCategory(id: number): Promise<void> {
  // some: 조건에 맞는 항목이 하나라도 있으면 true (C# LINQ의 Any)
  if (transactionsMock.some((t) => t.categoryId === id)) {
    throw new Error('거래에서 사용 중인 카테고리는 삭제할 수 없습니다.')
  }
  const index = categoriesMock.findIndex((c) => c.id === id)
  if (index === -1) throw new Error('카테고리를 찾을 수 없습니다.')
  categoriesMock.splice(index, 1)
}

// 같은 종류 안에서 이름이 겹치면 에러. exceptId: 이름 변경 시 자기 자신은 빼고 검사한다.
function assertUniqueName({ name, type }: NewCategory, exceptId?: number) {
  const duplicated = categoriesMock.some((c) => c.type === type && c.name === name && c.id !== exceptId)
  if (duplicated) throw new Error('같은 이름의 카테고리가 이미 있습니다.')
}
