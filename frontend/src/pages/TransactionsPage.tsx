import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useState } from 'react'
import MonthNavigator from '@/components/common/MonthNavigator'
import TransactionFormDialog from '@/components/transaction/TransactionFormDialog'
import DailyTransactionList from '@/components/transaction/DailyTransactionList'
import LedgerCalendar from '@/components/transaction/LedgerCalendar'
import LedgerSummary from '@/components/transaction/LedgerSummary'
import MonthlyBalanceTable from '@/components/transaction/MonthlyBalanceTable'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { FEATURES } from '@/config/features'
import { getAccounts } from '@/services/assetService'
import { categoriesQueryKey, getCategories } from '@/services/categoryService'
import {
  addTransaction,
  deleteTransaction,
  getMonthlyLedger,
  getYearlyLedger,
  monthlyLedgerQueryKey,
  transactionsQueryKey,
  updateTransaction,
  yearlyLedgerQueryKey,
} from '@/services/transactionService'
import type { NewTransaction, Transaction } from '@/types/transaction'
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
  const [view, setView] = useState<ViewMode>('list')

  // [useQuery] 데이터 불러오기 (설명은 lib/queryClient.ts, SettingsPage 참고)
  // 이름표에 year, month가 들어 있어서, 월을 바꾸면 이름표가 달라져 그 달 데이터를 새로 불러온다.
  // 한 번 본 달은 캐시에 남아 있어서 다시 가면 바로 보인다.
  const monthlyQuery = useQuery({
    queryKey: monthlyLedgerQueryKey(year, month),
    queryFn: () => getMonthlyLedger(year, month),
  })
  // 월별 탭의 연간 데이터. enabled: 월별 탭을 볼 때만 불러온다 (목록·달력에서는 필요 없는 요청을 보내지 않게)
  const yearlyQuery = useQuery({
    queryKey: yearlyLedgerQueryKey(year),
    queryFn: () => getYearlyLedger(year),
    enabled: view === 'monthly',
  })
  // 입력 창의 카테고리 목록. 설정 화면과 같은 이름표라 캐시를 함께 쓴다 (설정에서 바꾸면 여기도 바뀐다)
  const { data: categories = [] } = useQuery({ queryKey: categoriesQueryKey, queryFn: getCategories })
  // 계좌 목록은 계좌 기능이 켜졌을 때만 (결정 D16)
  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts, enabled: FEATURES.accounts })
  const queryClient = useQueryClient()

  const ledger = monthlyQuery.data
  const yearly = yearlyQuery.data

  // 달력에서 고른 날짜. 처음엔 오늘
  const [selectedDate, setSelectedDate] = useState<string | null>(getToday)
  // 다른 달로 넘어가면 고른 날짜는 그 달에 없으므로 "안 고름"으로 본다 (파생 값. 월 이동 때 따로 지우지 않아도 된다)
  const monthPrefix = toDateKey(yearMonth, 1).slice(0, 8) // 'YYYY-MM-' (1일 날짜에서 앞 8글자)
  const selectedDateInMonth = selectedDate?.startsWith(monthPrefix) ? selectedDate : null
  // 새 거래 날짜 칸의 처음 값:
  //   달력에서 날짜를 골라뒀으면 그 날짜 → 오늘이 보고 있는 달 안이면 오늘 → 아니면 보고 있는 달의 1일
  //   (지난달을 보면서 등록하면 대개 지난달 거래라서. 저장 후 화면이 갑자기 이번 달로 바뀌지 않게)
  const today = getToday()
  const newTransactionDate =
    (view === 'calendar' ? selectedDateInMonth : null) ??
    (today.startsWith(monthPrefix) ? today : toDateKey(yearMonth, 1))

  const [isFormOpen, setIsFormOpen] = useState(false)
  // 팝업에서 수정 중인 거래. null이면 새로 등록.
  // 닫을 때 이 값을 null로 되돌리지 않는다. 닫히는 애니메이션 동안 제목이 "거래 등록"으로 바뀌어 보이기 때문.
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)
  // 입력 창을 열 때마다 1씩 올려서 key로 넘긴다. [key] React는 key가 바뀌면 그 컴포넌트를 버리고 새로 만든다.
  // 이게 없으면 닫히는 애니메이션 도중에 다시 열 때 이전 입력 창이 재사용되어, 지난번 입력값(메모 등)이 남아 있었다.
  const [formKey, setFormKey] = useState(0)

  function openAddForm() {
    setEditingTransaction(null)
    setFormKey((prev) => prev + 1)
    setIsFormOpen(true)
  }

  function openEditForm(transaction: Transaction) {
    setEditingTransaction(transaction)
    setFormKey((prev) => prev + 1)
    setIsFormOpen(true)
  }

  // 거래가 바뀌면 월별·연간 데이터가 모두 낡은 것이 된다. 'transactions'로 시작하는 이름표를 한 번에 invalidate한다.
  async function refreshLedger() {
    await queryClient.invalidateQueries({ queryKey: transactionsQueryKey })
  }

  // 실패하면 service가 ApiError를 던지고, 입력 창(TransactionFormDialog)이 받아서 문구를 보여준다. 팝업은 닫지 않는다.
  async function handleSubmit(input: NewTransaction) {
    const saved =
      editingTransaction === null
        ? await addTransaction(input)
        : await updateTransaction(editingTransaction.id, input)
    setIsFormOpen(false)
    // 저장한 거래가 있는 달로 이동한다 (날짜를 다른 달로 고친 경우도 포함)
    const [savedYear, savedMonth] = saved.date.split('-').map(Number)
    setYearMonth({ year: savedYear, month: savedMonth })
    // 달력에서도 저장한 날짜가 선택되게 한다
    setSelectedDate(saved.date)
    await refreshLedger()
  }

  async function handleDelete() {
    // 삭제 버튼은 수정 중일 때만 보이므로 여기서 null일 일은 없지만, TypeScript에게 알려주기 위해 확인한다
    if (editingTransaction === null) return
    await deleteTransaction(editingTransaction.id)
    setIsFormOpen(false)
    await refreshLedger()
  }

  // 월별 표에서 달을 누르면: 그 달로 이동 + 목록 탭으로 전환
  function openMonth(selectedMonth: number) {
    setYearMonth({ year, month: selectedMonth })
    setView('list')
  }

  // 불러오기 실패 문구 (서버 꺼짐 등)
  const loadError = view === 'monthly' ? yearlyQuery.error : monthlyQuery.error

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

        {loadError ? (
          <p role="alert" className="text-destructive">
            {loadError.message}
          </p>
        ) : view === 'monthly' ? (
          <TabsContent value="monthly" className="flex flex-col gap-6">
            {yearly === undefined ? (
              <p className="text-muted-foreground">불러오는 중...</p>
            ) : (
              <>
                <LedgerSummary income={yearly.income} expense={yearly.expense} balance={yearly.balance} />
                <MonthlyBalanceTable months={yearly.months} onSelectMonth={openMonth} />
              </>
            )}
          </TabsContent>
        ) : ledger === undefined ? (
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
        key={formKey}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        transaction={editingTransaction}
        defaultDate={newTransactionDate}
        categories={categories}
        accounts={accounts}
        onSubmit={handleSubmit}
        onDelete={handleDelete}
      />
    </div>
  )
}

export default TransactionsPage
