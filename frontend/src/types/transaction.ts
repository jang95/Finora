// 가계부 거래. 계획서 8장 Transactions 테이블 + 목록 표시에 필요한 이름(Backend가 조인해서 내려줌).

// union 타입: 이 세 문자열 중 하나만 허용한다 (C#의 enum과 비슷한 역할)
export type TransactionType = 'Income' | 'Expense' | 'Transfer'

export interface Transaction {
  id: number
  date: string // 거래일 'YYYY-MM-DD' (한국 기준 날짜, 시간 없음)
  type: TransactionType
  accountId: number
  accountName: string
  // ?: 선택 속성. 이체일 때만 받는 계좌가 있고, 이체일 때는 카테고리가 없다.
  toAccountId?: number
  toAccountName?: string
  categoryId?: number
  categoryName?: string
  amount: number // 항상 양수. 수입/지출 구분은 type으로 한다.
  memo?: string
}
