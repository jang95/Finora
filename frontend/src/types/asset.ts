// 계좌(자산). 계획서 8장 Accounts 테이블 중 지금 화면에 필요한 것만. 잔액 등은 자산 화면을 만들 때 추가한다.
export type AccountType = 'Cash' | 'Bank' | 'Deposit' | 'Savings' | 'Other'

export interface Account {
  id: number
  name: string
  type: AccountType
}
