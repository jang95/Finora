import type { Account } from '@/types/asset'

// Phase 2용 가짜 계좌. id는 mocks/transactions.ts의 accountId와 맞춘다.
// 잔액 합계는 mocks/dashboard.ts의 총자산(52,340,000)과 맞춘다.
export const accountsMock: Account[] = [
  { id: 2, name: '지갑', type: 'Cash', balance: 220_000 },
  { id: 1, name: '월급통장', type: 'Bank', balance: 4_120_000 },
  { id: 4, name: '정기예금', type: 'Deposit', balance: 30_000_000 },
  { id: 3, name: '적금', type: 'Savings', balance: 18_000_000 },
]
