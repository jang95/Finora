import { apiRequest } from '@/services/api'
import type { MonthlyLedger, NewTransaction, Transaction, YearlyLedger } from '@/types/transaction'

// 가계부 거래 API (Phase 5). 합계·날짜별 묶음은 Backend가 decimal로 계산해서 내려준다 (결정 D4).
// 화면은 받은 값을 그리기만 한다.

// TanStack Query 캐시 이름표. 모든 거래 데이터의 이름표가 'transactions'로 시작한다.
// 거래를 등록·수정·삭제한 뒤 ['transactions']로 invalidate하면, 월별·연간 데이터가 모두 다시 불러와진다.
// (이름표는 앞부분만 같아도 함께 낡은 것으로 처리된다)
export const transactionsQueryKey = ['transactions'] as const

export const monthlyLedgerQueryKey = (year: number, month: number) => [...transactionsQueryKey, 'monthly', year, month] as const

export const yearlyLedgerQueryKey = (year: number) => [...transactionsQueryKey, 'yearly', year] as const

// 한 달: 월 요약(수입/지출/합계) + 날짜별 묶음과 그날 합계 (최신 날짜가 위)
export async function getMonthlyLedger(year: number, month: number): Promise<MonthlyLedger> {
  return apiRequest<MonthlyLedger>(`/api/transactions/monthly?year=${year}&month=${month}`)
}

// 한 해: 1~12월 합계 (월별 탭)
export async function getYearlyLedger(year: number): Promise<YearlyLedger> {
  return apiRequest<YearlyLedger>(`/api/transactions/yearly?year=${year}`)
}

// 등록. id와 카테고리 이름은 Backend가 채워서 돌려준다.
export async function addTransaction(input: NewTransaction): Promise<Transaction> {
  return apiRequest<Transaction>('/api/transactions', { method: 'POST', body: input })
}

// 수정. Backend는 이 거래가 로그인한 사용자 것인지 확인한 뒤 수정한다 (아니면 404).
export async function updateTransaction(id: number, input: NewTransaction): Promise<Transaction> {
  return apiRequest<Transaction>(`/api/transactions/${id}`, { method: 'PUT', body: input })
}

export async function deleteTransaction(id: number): Promise<void> {
  await apiRequest<void>(`/api/transactions/${id}`, { method: 'DELETE' })
}
