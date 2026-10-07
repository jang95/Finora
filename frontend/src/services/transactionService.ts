import { transactionsMock } from '@/mocks/transactions'
import type { Transaction } from '@/types/transaction'

// 월별 거래 목록 (최신 날짜가 위). month는 1~12.
// 지금은 mock에서 걸러서 돌려주고, Backend가 생기면 GET /api/transactions?year=&month= 호출로 바꾼다.
export async function getTransactions(year: number, month: number): Promise<Transaction[]> {
  // padStart: 앞을 '0'으로 채워 2자리로 만든다. 10 → '10', 7 → '07'
  const prefix = `${year}-${String(month).padStart(2, '0')}-`

  return transactionsMock
    .filter((t) => t.date.startsWith(prefix))
    // 'YYYY-MM-DD' 문자열은 사전순 = 날짜순이라 문자열 비교로 정렬할 수 있다. 같은 날은 나중에 등록한 것(id 큰 것)이 위.
    .sort((a, b) => b.date.localeCompare(a.date) || b.id - a.id)
}
