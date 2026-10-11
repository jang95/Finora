import { Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import MonthNavigator from '@/components/common/MonthNavigator'
import TransactionFormDialog from '@/components/transaction/TransactionFormDialog'
import DailyTransactionList from '@/components/transaction/DailyTransactionList'
import LedgerSummary from '@/components/transaction/LedgerSummary'
import { Button } from '@/components/ui/button'
import { getAccounts } from '@/services/assetService'
import { getCategories } from '@/services/categoryService'
import {
  addTransaction,
  deleteTransaction,
  getMonthlyLedger,
  updateTransaction,
} from '@/services/transactionService'
import type { Account } from '@/types/asset'
import type { Category } from '@/types/category'
import type { MonthlyLedger, NewTransaction, Transaction } from '@/types/transaction'
import { getThisMonth, type YearMonth } from '@/utils/yearMonth'

function TransactionsPage() {
  // useState(getThisMonth): 함수 자체를 넘기면 처음 한 번만 실행된다 (getThisMonth()로 넘기면 그릴 때마다 실행됨)
  const [yearMonth, setYearMonth] = useState<YearMonth>(getThisMonth)
  const { year, month } = yearMonth

  // 선택한 달의 요약 + 날짜별 거래
  const [ledger, setLedger] = useState<MonthlyLedger | null>(null)
  // 목록을 다시 불러오고 싶을 때 1씩 올린다. 아래 useEffect가 이 값이 바뀐 것을 보고 다시 실행된다.
  const [reloadKey, setReloadKey] = useState(0)

  const [categories, setCategories] = useState<Category[]>([])
  const [accounts, setAccounts] = useState<Account[]>([])
  const [isFormOpen, setIsFormOpen] = useState(false)
  // 팝업에서 수정 중인 거래. null이면 새로 등록.
  // 닫을 때 이 값을 null로 되돌리지 않는다. 닫히는 애니메이션 동안 제목이 "거래 등록"으로 바뀌어 보이기 때문.
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)

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

    getMonthlyLedger(year, month).then((data) => {
      if (!ignore) setLedger(data)
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
        <MonthNavigator value={yearMonth} onChange={setYearMonth} />
      </div>

      {/* self-end: 이 요소만 오른쪽 끝에 붙인다 */}
      <Button className="self-end" onClick={openAddForm}>
        <Plus />
        거래 등록
      </Button>

      {ledger === null ? (
        <p className="text-muted-foreground">불러오는 중...</p>
      ) : (
        <>
          <LedgerSummary income={ledger.income} expense={ledger.expense} balance={ledger.balance} />
          <DailyTransactionList days={ledger.days} onSelect={openEditForm} />
        </>
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

export default TransactionsPage
