import type { Transaction } from '@/types/transaction'

// Phase 2용 가짜 거래내역. services/만 import한다. (실제 금융 데이터를 넣지 않는다)
export const transactionsMock: Transaction[] = [
  { id: 1, date: '2026-10-01', type: 'Income', accountId: 1, accountName: '월급통장', categoryId: 1, categoryName: '급여', amount: 3_800_000 },
  { id: 2, date: '2026-10-01', type: 'Transfer', accountId: 1, accountName: '월급통장', toAccountId: 3, toAccountName: '적금', amount: 500_000, memo: '자동이체' },
  { id: 3, date: '2026-10-02', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 3, categoryName: '주거', amount: 650_000, memo: '월세' },
  { id: 4, date: '2026-10-03', type: 'Expense', accountId: 2, accountName: '지갑', categoryId: 2, categoryName: '식비', amount: 12_000, memo: '점심' },
  { id: 5, date: '2026-10-05', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 4, categoryName: '교통', amount: 55_000, memo: '교통카드 충전' },
  { id: 6, date: '2026-10-06', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 2, categoryName: '식비', amount: 87_500, memo: '장보기' },
  { id: 7, date: '2026-10-07', type: 'Expense', accountId: 2, accountName: '지갑', categoryId: 2, categoryName: '식비', amount: 4_500, memo: '커피' },
  { id: 8, date: '2026-09-25', type: 'Income', accountId: 1, accountName: '월급통장', categoryId: 1, categoryName: '급여', amount: 3_800_000 },
  { id: 9, date: '2026-09-28', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 2, categoryName: '식비', amount: 32_000 },
]
