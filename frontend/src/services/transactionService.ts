import { accountsMock } from '@/mocks/accounts'
import { categoriesMock } from '@/mocks/categories'
import { transactionsMock } from '@/mocks/transactions'
import type { NewTransaction, Transaction } from '@/types/transaction'

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

// 거래 등록. 지금은 mock 배열에 추가만 한다 (새로고침하면 사라짐).
// Backend가 생기면 POST /api/transactions 호출로 바꾸고, id와 이름은 Backend가 채워서 돌려준다.
export async function addTransaction(input: NewTransaction): Promise<Transaction> {
  // Math.max(...배열): 배열을 펼쳐서 가장 큰 값. DB의 자동 증가 id 흉내
  const id = Math.max(0, ...transactionsMock.map((t) => t.id)) + 1

  const created: Transaction = {
    // ...input: input의 속성을 그대로 복사해 넣는다 (스프레드 문법)
    ...input,
    id,
    accountName: findAccountName(input.accountId),
    toAccountName: input.toAccountId === undefined ? undefined : findAccountName(input.toAccountId),
    categoryName: categoriesMock.find((c) => c.id === input.categoryId)?.name,
  }

  transactionsMock.push(created)
  return created
}

// ?.: 앞의 값이 undefined면 에러 없이 undefined를 돌려준다. ??: 앞이 undefined/null이면 뒤의 값을 쓴다.
function findAccountName(accountId: number): string {
  return accountsMock.find((a) => a.id === accountId)?.name ?? ''
}
