import { accountsMock } from '@/mocks/accounts'
import { categoriesMock } from '@/mocks/categories'
import { transactionsMock } from '@/mocks/transactions'
import type { DailyTransactions, MonthlyLedger, NewTransaction, Transaction } from '@/types/transaction'

// 월별 거래 목록 (최신 날짜가 위). month는 1~12.
// 지금은 mock에서 걸러서 돌려주고, Backend가 생기면 GET /api/transactions?year=&month= 호출로 바꾼다.
export async function getTransactions(year: number, month: number): Promise<Transaction[]> {
  // padStart: 앞을 '0'으로 채워 2자리로 만든다. 10 → '10', 7 → '07'
  const prefix = `${year}-${String(month).padStart(2, '0')}-`

  return transactionsMock.filter((t) => t.date.startsWith(prefix)).sort(byNewest)
}

// 가계부 한 달 화면: 월 요약(수입/지출/합계) + 날짜별 묶음과 그날 합계.
// 지금은 여기서 합계를 내고, Backend가 생기면 GET /api/transactions/monthly?year=&month= 호출로 바꾼다. (결정 D4)
export async function getMonthlyLedger(year: number, month: number): Promise<MonthlyLedger> {
  const transactions = await getTransactions(year, month)

  // 날짜별로 묶는다. 목록이 이미 최신 날짜순이라 Map에 넣은 순서(= 처음 나온 순서)도 최신 날짜순이 된다.
  // (Map은 넣은 순서를 기억한다. C#의 Dictionary는 순서를 보장하지 않는 것과 다르다)
  const byDate = new Map<string, DailyTransactions>()
  for (const t of transactions) {
    let day = byDate.get(t.date)
    if (day === undefined) {
      day = { date: t.date, income: 0, expense: 0, transactions: [] }
      byDate.set(t.date, day)
    }
    day.transactions.push(t)
    if (t.type === 'Income') day.income += t.amount
    if (t.type === 'Expense') day.expense += t.amount
  }

  const days = [...byDate.values()]
  const income = sumBy(days, (d) => d.income)
  const expense = sumBy(days, (d) => d.expense)
  return { income, expense, balance: income - expense, days }
}

// 최근 거래 count건 (최신이 위). 대시보드용.
// Backend가 생기면 대시보드 API가 함께 내려주므로 이 함수는 dashboardService 안에서만 쓴다.
export async function getRecentTransactions(count: number): Promise<Transaction[]> {
  // [...배열]로 복사한 뒤 정렬한다. sort는 원본 배열 순서를 바꿔버리기 때문 (mock 원본은 그대로 둔다)
  // slice(0, count): 앞에서 count개만 잘라낸 새 배열
  return [...transactionsMock].sort(byNewest).slice(0, count)
}

// 정렬 기준: 'YYYY-MM-DD' 문자열은 사전순 = 날짜순이라 문자열 비교로 정렬할 수 있다. 같은 날은 나중에 등록한 것(id 큰 것)이 위.
function byNewest(a: Transaction, b: Transaction): number {
  return b.date.localeCompare(a.date) || b.id - a.id
}

// 배열의 각 항목에서 숫자를 골라(pick) 더한다. pick은 "항목 → 숫자" 함수 (C# LINQ의 Sum(x => x.Income)과 같다)
// <T>: 제네릭. 어떤 타입의 배열이든 받을 수 있고, pick의 매개변수 타입도 그 배열의 항목 타입으로 맞춰진다 (C#의 제네릭과 같다)
function sumBy<T>(items: T[], pick: (item: T) => number): number {
  return items.reduce((sum, item) => sum + pick(item), 0)
}

// 거래 등록. 지금은 mock 배열에 추가만 한다 (새로고침하면 사라짐).
// Backend가 생기면 POST /api/transactions 호출로 바꾸고, id와 이름은 Backend가 채워서 돌려준다.
export async function addTransaction(input: NewTransaction): Promise<Transaction> {
  // Math.max(...배열): 배열을 펼쳐서 가장 큰 값. DB의 자동 증가 id 흉내
  const id = Math.max(0, ...transactionsMock.map((t) => t.id)) + 1
  const created = toTransaction(id, input)
  transactionsMock.push(created)
  return created
}

// 거래 수정. Backend가 생기면 PUT /api/transactions/{id} 호출로 바꾼다.
// (Backend는 이 거래가 로그인한 사용자 것인지 확인한 뒤 수정한다)
export async function updateTransaction(id: number, input: NewTransaction): Promise<Transaction> {
  const index = transactionsMock.findIndex((t) => t.id === id)
  // findIndex는 못 찾으면 -1. 실제 API라면 404 응답에 해당한다
  // throw: 에러를 던져 함수를 그 자리에서 멈춘다 (C#의 throw new Exception과 같다).
  //   async 함수에서 던지면 반환된 Promise가 "실패"가 되고, 부르는 쪽의 await에서 에러가 다시 나타난다.
  if (index === -1) throw new Error('거래를 찾을 수 없습니다.')

  const updated = toTransaction(id, input)
  // 기존 객체의 속성을 하나씩 바꾸지 않고 새 객체로 통째로 교체한다.
  // 수정 전에 없던 속성(예: memo를 지운 경우)이 이전 값으로 남는 실수를 막는다.
  transactionsMock[index] = updated
  return updated
}

// 거래 삭제. Backend가 생기면 DELETE /api/transactions/{id} 호출로 바꾼다.
export async function deleteTransaction(id: number): Promise<void> {
  const index = transactionsMock.findIndex((t) => t.id === id)
  if (index === -1) throw new Error('거래를 찾을 수 없습니다.')
  // splice(위치, 개수): 배열에서 해당 위치의 항목을 지운다
  transactionsMock.splice(index, 1)
}

// 입력값 + id → 목록에 표시할 거래. 이름(accountName 등)을 채우는 일은 나중에 Backend가 조인으로 한다.
function toTransaction(id: number, input: NewTransaction): Transaction {
  return {
    // ...input: input의 속성을 그대로 복사해 넣는다 (스프레드 문법)
    ...input,
    id,
    accountName: findAccountName(input.accountId),
    toAccountName: input.toAccountId === undefined ? undefined : findAccountName(input.toAccountId),
    categoryName: categoriesMock.find((c) => c.id === input.categoryId)?.name,
  }
}

// ?.: 앞의 값이 undefined면 에러 없이 undefined를 돌려준다. ??: 앞이 undefined/null이면 뒤의 값을 쓴다.
function findAccountName(accountId: number): string {
  return accountsMock.find((a) => a.id === accountId)?.name ?? ''
}
