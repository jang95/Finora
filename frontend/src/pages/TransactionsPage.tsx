import { Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import MonthNavigator from '@/components/common/MonthNavigator'
import TransactionFormDialog from '@/components/transaction/TransactionFormDialog'
import DailyTransactionList from '@/components/transaction/DailyTransactionList'
import LedgerCalendar from '@/components/transaction/LedgerCalendar'
import LedgerSummary from '@/components/transaction/LedgerSummary'
import MonthlyBalanceTable from '@/components/transaction/MonthlyBalanceTable'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { getAccounts } from '@/services/assetService'
import { getCategories } from '@/services/categoryService'
import {
  addTransaction,
  deleteTransaction,
  getMonthlyLedger,
  getYearlyLedger,
  updateTransaction,
} from '@/services/transactionService'
import type { Account } from '@/types/asset'
import type { Category } from '@/types/category'
import type { MonthlyLedger, NewTransaction, Transaction, YearlyLedger } from '@/types/transaction'
import { getThisMonth, getToday, toDateKey, type YearMonth } from '@/utils/yearMonth'

// 가계부 보기 방식. 목록·달력은 한 달 단위, 월별은 한 해의 12개월 요약
type ViewMode = 'list' | 'calendar' | 'monthly'

const VIEW_OPTIONS: { value: ViewMode; label: string }[] = [
  { value: 'list', label: '목록' },
  { value: 'calendar', label: '달력' },
  { value: 'monthly', label: '월별' },
]

function TransactionsPage() {
  // useState(getThisMonth): 함수 자체를 넘기면 처음 한 번만 실행된다 (getThisMonth()로 넘기면 그릴 때마다 실행됨)
  const [yearMonth, setYearMonth] = useState<YearMonth>(getThisMonth)
  const { year, month } = yearMonth

  // 선택한 달의 요약 + 날짜별 거래
  const [ledger, setLedger] = useState<MonthlyLedger | null>(null)
  // 선택한 해의 월별 요약 (월별 탭)
  const [yearly, setYearly] = useState<YearlyLedger | null>(null)
  // 목록을 다시 불러오고 싶을 때 1씩 올린다. 아래 useEffect가 이 값이 바뀐 것을 보고 다시 실행된다.
  const [reloadKey, setReloadKey] = useState(0)

  const [categories, setCategories] = useState<Category[]>([])
  const [accounts, setAccounts] = useState<Account[]>([])
  const [view, setView] = useState<ViewMode>('list')
  // 달력에서 고른 날짜. 처음엔 오늘
  const [selectedDate, setSelectedDate] = useState<string | null>(getToday)
  // 다른 달로 넘어가면 고른 날짜는 그 달에 없으므로 "안 고름"으로 본다 (파생 값. 월 이동 때 따로 지우지 않아도 된다)
  const monthPrefix = toDateKey(yearMonth, 1).slice(0, 8) // 'YYYY-MM-' (1일 날짜에서 앞 8글자)
  const selectedDateInMonth = selectedDate?.startsWith(monthPrefix) ? selectedDate : null

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
    // 달력에서도 저장한 날짜가 선택되게 한다
    setSelectedDate(saved.date)
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

  // 월별 탭의 연간 데이터. 월별 탭을 볼 때만 불러온다 (목록·달력에서는 필요 없는 요청을 보내지 않게)
  useEffect(() => {
    if (view !== 'monthly') return
    let ignore = false

    getYearlyLedger(year).then((data) => {
      if (!ignore) setYearly(data)
    })

    return () => {
      ignore = true
    }
    // month는 넣지 않는다. 연간 데이터는 해가 바뀔 때만 달라진다
  }, [view, year, reloadKey])

  // 월별 표에서 달을 누르면: 그 달로 이동 + 목록 탭으로 전환
  function openMonth(selectedMonth: number) {
    setYearMonth({ year, month: selectedMonth })
    setView('list')
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-bold">가계부</h1>
        {/* 월별 탭에서는 한 해씩 이동한다 */}
        <MonthNavigator value={yearMonth} onChange={setYearMonth} unit={view === 'monthly' ? 'year' : 'month'} />
      </div>

      {/* [Tabs] shadcn(Radix) 탭. value(지금 탭)는 우리 state가 쥐고, 탭을 누르면 onValueChange로 알려준다 (제어 컴포넌트).
            TabsContent는 value가 같을 때만 화면에 그려진다.
          onValueChange는 string을 주므로 ViewMode로 바꿔서 넣는다 (as: "이 값은 이 타입이다"라고 TypeScript에 알려주는 것.
            탭 value가 VIEW_OPTIONS에서만 나오므로 안전하다) */}
      <Tabs value={view} onValueChange={(value) => setView(value as ViewMode)} className="gap-6">
        <div className="flex items-center justify-between gap-4">
          <TabsList>
            {VIEW_OPTIONS.map((option) => (
              <TabsTrigger key={option.value} value={option.value}>
                {option.label}
              </TabsTrigger>
            ))}
          </TabsList>
          <Button onClick={openAddForm}>
            <Plus />
            거래 등록
          </Button>
        </div>

        {view === 'monthly' ? (
          <TabsContent value="monthly" className="flex flex-col gap-6">
            {yearly === null ? (
              <p className="text-muted-foreground">불러오는 중...</p>
            ) : (
              <>
                <LedgerSummary income={yearly.income} expense={yearly.expense} balance={yearly.balance} />
                <MonthlyBalanceTable months={yearly.months} onSelectMonth={openMonth} />
              </>
            )}
          </TabsContent>
        ) : ledger === null ? (
          <p className="text-muted-foreground">불러오는 중...</p>
        ) : (
          <>
            <LedgerSummary income={ledger.income} expense={ledger.expense} balance={ledger.balance} />

            <TabsContent value="list">
              <DailyTransactionList days={ledger.days} onSelect={openEditForm} />
            </TabsContent>

            <TabsContent value="calendar" className="flex flex-col gap-4">
              <LedgerCalendar
                yearMonth={yearMonth}
                days={ledger.days}
                selectedDate={selectedDateInMonth}
                onSelectDate={setSelectedDate}
              />
              {selectedDateInMonth === null ? (
                <p className="text-sm text-muted-foreground">날짜를 누르면 그날 거래를 볼 수 있습니다.</p>
              ) : (
                // 고른 날의 묶음만 넘긴다. 거래가 없는 날이면 빈 배열 → "거래 내역이 없습니다."
                <DailyTransactionList
                  days={ledger.days.filter((d) => d.date === selectedDateInMonth)}
                  onSelect={openEditForm}
                />
              )}
            </TabsContent>
          </>
        )}
      </Tabs>

      <TransactionFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        transaction={editingTransaction}
        // 달력에서 날짜를 골라둔 상태로 등록하면 그 날짜로 시작한다
        defaultDate={view === 'calendar' ? (selectedDateInMonth ?? undefined) : undefined}
        categories={categories}
        accounts={accounts}
        onSubmit={handleSubmit}
        onDelete={handleDelete}
      />
    </div>
  )
}

export default TransactionsPage
