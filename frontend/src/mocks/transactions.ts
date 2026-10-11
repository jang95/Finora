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
  // 통계 화면(최근 6개월 그래프)용 지난달 데이터
  { id: 10, date: '2026-09-02', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 3, categoryName: '주거', amount: 650_000, memo: '월세' },
  { id: 11, date: '2026-09-14', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 6, categoryName: '쇼핑', amount: 129_000 },
  { id: 12, date: '2026-09-20', type: 'Expense', accountId: 2, accountName: '지갑', categoryId: 2, categoryName: '식비', amount: 210_000 },
  { id: 13, date: '2026-08-25', type: 'Income', accountId: 1, accountName: '월급통장', categoryId: 1, categoryName: '급여', amount: 3_800_000 },
  { id: 14, date: '2026-08-02', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 3, categoryName: '주거', amount: 650_000, memo: '월세' },
  { id: 15, date: '2026-08-09', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 2, categoryName: '식비', amount: 420_000 },
  { id: 16, date: '2026-08-15', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 6, categoryName: '쇼핑', amount: 860_000, memo: '여름 휴가' },
  { id: 17, date: '2026-07-25', type: 'Income', accountId: 1, accountName: '월급통장', categoryId: 1, categoryName: '급여', amount: 3_800_000 },
  { id: 18, date: '2026-07-10', type: 'Income', accountId: 2, accountName: '지갑', categoryId: 5, categoryName: '용돈', amount: 300_000 },
  { id: 19, date: '2026-07-02', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 3, categoryName: '주거', amount: 650_000, memo: '월세' },
  { id: 20, date: '2026-07-18', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 2, categoryName: '식비', amount: 385_000 },
  { id: 21, date: '2026-06-25', type: 'Income', accountId: 1, accountName: '월급통장', categoryId: 1, categoryName: '급여', amount: 3_800_000 },
  { id: 22, date: '2026-06-02', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 3, categoryName: '주거', amount: 650_000, memo: '월세' },
  { id: 23, date: '2026-06-21', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 2, categoryName: '식비', amount: 356_000 },
  { id: 24, date: '2026-06-28', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 4, categoryName: '교통', amount: 62_000 },
  { id: 25, date: '2026-05-25', type: 'Income', accountId: 1, accountName: '월급통장', categoryId: 1, categoryName: '급여', amount: 3_800_000 },
  { id: 26, date: '2026-05-02', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 3, categoryName: '주거', amount: 650_000, memo: '월세' },
  { id: 27, date: '2026-05-08', type: 'Expense', accountId: 1, accountName: '월급통장', categoryId: 6, categoryName: '쇼핑', amount: 240_000, memo: '어버이날' },
  { id: 28, date: '2026-05-16', type: 'Expense', accountId: 2, accountName: '지갑', categoryId: 2, categoryName: '식비', amount: 298_000 },
]
