import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import MoneyText from '@/components/common/MoneyText'
import TransactionFormDialog from '@/components/transaction/TransactionFormDialog'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import { getAccounts } from '@/services/assetService'
import { getCategories } from '@/services/categoryService'
import {
  addTransaction,
  deleteTransaction,
  getTransactions,
  updateTransaction,
} from '@/services/transactionService'
import type { Account } from '@/types/asset'
import type { Category } from '@/types/category'
import type { NewTransaction, Transaction } from '@/types/transaction'
import { formatDate } from '@/utils/format'

// 연·월을 한 묶음으로 다룬다. 따로 state 2개로 두면 12월 → 1월처럼 둘이 같이 바뀔 때 실수하기 쉽다.
interface YearMonth {
  year: number
  month: number // 1~12
}

// 이번 달. getMonth()는 0~11이라 +1
function getThisMonth(): YearMonth {
  const today = new Date()
  return { year: today.getFullYear(), month: today.getMonth() + 1 }
}

// offset만큼 월을 이동한다 (-1: 이전 달, +1: 다음 달). 연도 넘어가는 것도 처리한다.
function addMonths({ year, month }: YearMonth, offset: number): YearMonth {
  // new Date(2026, 12, 1)처럼 범위를 넘는 월을 넣으면 Date가 알아서 2027년 1월로 바꿔준다
  const date = new Date(year, month - 1 + offset, 1)
  return { year: date.getFullYear(), month: date.getMonth() + 1 }
}

function TransactionsPage() {
  // useState(getThisMonth): 함수 자체를 넘기면 처음 한 번만 실행된다 (getThisMonth()로 넘기면 그릴 때마다 실행됨)
  const [{ year, month }, setYearMonth] = useState<YearMonth>(getThisMonth)

  const [transactions, setTransactions] = useState<Transaction[] | null>(null)
  // 목록을 다시 불러오고 싶을 때 1씩 올린다. 아래 useEffect가 이 값이 바뀐 것을 보고 다시 실행된다.
  const [reloadKey, setReloadKey] = useState(0)

  const [categories, setCategories] = useState<Category[]>([])
  const [accounts, setAccounts] = useState<Account[]>([])
  const [isFormOpen, setIsFormOpen] = useState(false)
  // 팝업에서 수정 중인 거래. null이면 새로 등록.
  // 닫을 때 이 값을 null로 되돌리지 않는다. 닫히는 애니메이션 동안 제목이 "거래 등록"으로 바뀌어 보이기 때문.
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)

  // 이전 값(prev)을 받아 새 값을 만드는 형태. 버튼을 빠르게 여러 번 눌러도 항상 최신 값 기준으로 계산된다.
  function moveMonth(offset: number) {
    setYearMonth((prev) => addMonths(prev, offset))
  }

  // 등록 폼의 선택 목록. 월이 바뀌어도 그대로이므로 처음 한 번만 불러온다 (의존성 배열 [])
  useEffect(() => {
    // Promise.all: 두 요청을 동시에 보내고 둘 다 끝나면 결과를 배열로 받는다
    Promise.all([getCategories(), getAccounts()]).then(([categoryList, accountList]) => {
      setCategories(categoryList)
      setAccounts(accountList)
    })
  }, [])

  function openAddForm() {
    setEditingTransaction(null)
    setIsFormOpen(true)
  }

  function openEditForm(transaction: Transaction) {
    setEditingTransaction(transaction)
    setIsFormOpen(true)
  }

  async function handleSubmit(input: NewTransaction) {
    const saved =
      editingTransaction === null
        ? await addTransaction(input)
        : await updateTransaction(editingTransaction.id, input)
    setIsFormOpen(false)
    // 저장한 거래가 있는 달로 이동하고 목록을 다시 불러온다 (날짜를 다른 달로 고친 경우도 포함)
    const [savedYear, savedMonth] = saved.date.split('-').map(Number)
    setYearMonth({ year: savedYear, month: savedMonth })
    setReloadKey((prev) => prev + 1)
  }

  async function handleDelete() {
    // 삭제 버튼은 수정 중일 때만 보이므로 여기서 null일 일은 없지만, TypeScript에게 알려주기 위해 확인한다
    if (editingTransaction === null) return
    await deleteTransaction(editingTransaction.id)
    setIsFormOpen(false)
    setReloadKey((prev) => prev + 1)
  }

  useEffect(() => {
    let ignore = false

    getTransactions(year, month).then((data) => {
      if (!ignore) setTransactions(data)
    })

    return () => {
      ignore = true
    }
    // year, month가 바뀌면(월 이동) 다시 불러온다.
    // 이때 이전 effect의 cleanup이 먼저 실행돼 ignore = true가 되므로, 늦게 온 이전 달 응답은 무시된다.
  }, [year, month, reloadKey])

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-bold">가계부</h1>
        <div className="flex items-center gap-1">
          {/* 아이콘만 있는 버튼은 화면낭독기가 읽을 이름이 없으므로 aria-label을 붙인다 */}
          <Button variant="ghost" size="icon" aria-label="이전 달" onClick={() => moveMonth(-1)}>
            <ChevronLeft />
          </Button>
          <span className="min-w-28 text-center font-medium tabular-nums">
            {year}년 {month}월
          </span>
          <Button variant="ghost" size="icon" aria-label="다음 달" onClick={() => moveMonth(1)}>
            <ChevronRight />
          </Button>
        </div>
      </div>

      {/* self-end: 이 요소만 오른쪽 끝에 붙인다 */}
      <Button className="self-end" onClick={openAddForm}>
        <Plus />
        거래 등록
      </Button>

      {transactions === null ? (
        <p className="text-muted-foreground">불러오는 중...</p>
      ) : transactions.length === 0 ? (
        <p className="text-muted-foreground">거래 내역이 없습니다.</p>
      ) : (
        <Card className="gap-0 py-0">
          {/* divide-y: 항목 "사이"에만 선을 긋는다 (마지막 항목 아래는 선 없음) */}
          <ul className="divide-y">
            {/* map: 배열의 각 항목을 화면 요소로 바꾼다. C# LINQ의 Select와 같다.
                key: React가 목록에서 어떤 항목이 추가·삭제·이동됐는지 구분하는 값. 항목마다 고유해야 하므로 id를 쓴다. */}
            {transactions.map((t, index) => (
              <li key={t.id}>
                {/* 날짜가 바뀌는 첫 항목에만 날짜 제목을 붙인다 (목록은 날짜순으로 정렬돼 있음) */}
                {(index === 0 || transactions[index - 1].date !== t.date) && (
                  <div className="border-b bg-muted/50 px-4 py-2 text-xs font-medium text-muted-foreground">
                    {formatDate(t.date)}
                  </div>
                )}
                {/* 이체는 아직 폼에서 다루지 않으므로 누를 수 없게 한다 (이체 등록 단계에서 추가) */}
                <TransactionRow
                  transaction={t}
                  onClick={t.type === 'Transfer' ? undefined : () => openEditForm(t)}
                />
              </li>
            ))}
          </ul>
        </Card>
      )}

      <TransactionFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        transaction={editingTransaction}
        categories={categories}
        accounts={accounts}
        onSubmit={handleSubmit}
        onDelete={handleDelete}
      />
    </div>
  )
}

// 거래 한 줄. 이 화면에서만 쓰므로 같은 파일에 둔다. (다른 화면에서도 쓰게 되면 components/transaction/으로 옮긴다)
// onClick이 있으면 눌러서 수정할 수 있는 버튼으로 그린다.
function TransactionRow({ transaction: t, onClick }: { transaction: Transaction; onClick?: () => void }) {
  const isTransfer = t.type === 'Transfer'
  const className = 'flex w-full items-center justify-between gap-4 px-4 py-3 text-left'

  const content = (
    <>
      <div className="min-w-0">
        <p className="font-medium">{isTransfer ? '이체' : t.categoryName}</p>
        {/* truncate: 너무 길면 한 줄에서 ...으로 자른다 (모바일 대비) */}
        <p className="truncate text-sm text-muted-foreground">
          {isTransfer ? `${t.accountName} → ${t.toAccountName}` : t.accountName}
          {t.memo && ` · ${t.memo}`}
        </p>
      </div>
      <MoneyText
        amount={t.amount}
        tone={t.type === 'Income' ? 'income' : t.type === 'Expense' ? 'expense' : undefined}
        className="shrink-0 font-semibold"
      />
    </>
  )

  // 누를 수 있는 것은 div가 아니라 button으로 만든다. 그래야 키보드(Tab, Enter)로도 선택되고 화면낭독기가 "버튼"이라고 읽는다.
  if (onClick) {
    return (
      <button type="button" className={cn(className, 'hover:bg-muted/50')} onClick={onClick}>
        {content}
      </button>
    )
  }
  return <div className={className}>{content}</div>
}

export default TransactionsPage
