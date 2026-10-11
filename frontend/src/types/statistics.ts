// 통계 화면 응답. 합계·비율은 Backend가 decimal로 계산해서 내려준다 (결정 D4)

// 한 달의 수입/지출 합계 (이체는 포함하지 않음)
export interface MonthlyTotal {
  year: number
  month: number // 1~12
  income: number
  expense: number
}

// 한 카테고리의 지출 합계와 그달 전체 지출 중 비율
export interface CategoryExpense {
  categoryId: number
  categoryName: string
  amount: number
  ratio: number // 비율(%) 예: 35.5
}
