// 수입/지출 카테고리. 계획서 8장 Categories 테이블. (이체는 카테고리가 없다)
export type CategoryType = 'Income' | 'Expense'

// 최대 3단계: 대분류 › 중분류 › 소분류 (결정 D23)
export const MAX_CATEGORY_DEPTH = 3

export interface Category {
  id: number
  name: string
  type: CategoryType
  // 상위 카테고리 id. 대분류는 null. (Backend가 평평한 목록으로 보내고, utils/categoryTree.ts가 트리로 만든다)
  parentId: number | null
}

// 카테고리 추가 입력값. id는 Backend가 정한다.
// parentId가 있으면 그 아래 하위 분류로 만든다. 하위 분류의 종류는 Backend가 상위 카테고리를 따라 정한다.
export interface NewCategory {
  name: string
  type: CategoryType
  parentId?: number
}
