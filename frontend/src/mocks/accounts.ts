import type { Account } from '@/types/asset'

// Phase 2용 가짜 계좌. 계좌 Backend(Phase 8)가 생기면 지운다 (결정 D16).
// 자산 화면(지금은 "준비 중"으로 숨김)의 총잔액이 이 잔액들의 합으로 계산된다.
export const accountsMock: Account[] = [
  { id: 2, name: '지갑', type: 'Cash', balance: 220_000 },
  { id: 1, name: '월급통장', type: 'Bank', balance: 4_120_000 },
  { id: 4, name: '정기예금', type: 'Deposit', balance: 30_000_000 },
  { id: 3, name: '적금', type: 'Savings', balance: 18_000_000 },
]
