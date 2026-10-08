// 계좌(자산). 계획서 8장 Accounts 테이블 중 지금 화면에 필요한 것만.
export type AccountType = 'Cash' | 'Bank' | 'Deposit' | 'Savings' | 'Other'

export interface Account {
  id: number
  name: string
  type: AccountType
  balance: number // 현재 잔액
}

// 자산 화면 응답. 합계는 Backend가 decimal로 계산해서 내려준다 (결정 D4)
export interface AssetSummary {
  totalBalance: number
  accounts: Account[]
}
