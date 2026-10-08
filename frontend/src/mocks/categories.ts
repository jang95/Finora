import type { Category } from '@/types/category'

// Phase 2용 가짜 카테고리. id는 mocks/transactions.ts의 categoryId와 맞춘다.
export const categoriesMock: Category[] = [
  { id: 1, name: '급여', type: 'Income' },
  { id: 5, name: '용돈', type: 'Income' },
  { id: 2, name: '식비', type: 'Expense' },
  { id: 3, name: '주거', type: 'Expense' },
  { id: 4, name: '교통', type: 'Expense' },
  { id: 6, name: '쇼핑', type: 'Expense' },
]
