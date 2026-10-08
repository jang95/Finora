import type { Account } from '@/types/asset'

// Phase 2용 가짜 계좌. id는 mocks/transactions.ts의 accountId와 맞춘다.
export const accountsMock: Account[] = [
  { id: 1, name: '월급통장', type: 'Bank' },
  { id: 2, name: '지갑', type: 'Cash' },
  { id: 3, name: '적금', type: 'Savings' },
]
