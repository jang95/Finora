// 수입/지출 카테고리. 계획서 8장 Categories 테이블. (이체는 카테고리가 없다)
export type CategoryType = 'Income' | 'Expense'

export interface Category {
  id: number
  name: string
  type: CategoryType
}

// 카테고리 추가 입력값. id는 Backend가 정한다.
export interface NewCategory {
  name: string
  type: CategoryType
}
