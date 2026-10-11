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

// 등록할 때 보내는 값. id와 이름(accountName 등)은 저장하는 쪽(지금은 service, 나중엔 Backend)이 채운다.
// Omit<T, 키들>: T에서 해당 속성만 뺀 타입을 만든다. 같은 속성을 두 번 적지 않아도 된다.
export type NewTransaction = Omit<Transaction, 'id' | 'accountName' | 'toAccountName' | 'categoryName'>

// 하루치 거래와 그날 합계 (이체는 합계에 넣지 않음). 가계부 목록의 날짜 묶음과 달력 칸에 쓴다.
export interface DailyTransactions {
  date: string // 'YYYY-MM-DD'
  income: number
  expense: number
  transactions: Transaction[] // 같은 날 안에서 최신(나중 등록)이 위
}

// 가계부 한 달 화면 응답: 월 요약 + 날짜별 묶음 (최신 날짜가 위)
// Backend가 생기면 GET /api/transactions/monthly?year=&month= 응답 형태가 된다. 합계는 Backend decimal로 계산한다 (결정 D4)
export interface MonthlyLedger {
  income: number
  expense: number
  balance: number // 수입 - 지출 (음수면 지출이 더 많음)
  days: DailyTransactions[]
}
