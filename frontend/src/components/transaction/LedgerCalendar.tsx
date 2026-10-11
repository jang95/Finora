import { cn } from '@/lib/utils'
import type { DailyTransactions } from '@/types/transaction'
import { formatAmount, formatMoney, formatMoneyCompact } from '@/utils/format'
import { getDaysInMonth, getFirstWeekday, getToday, toDateKey, type YearMonth } from '@/utils/yearMonth'

const WEEKDAYS = ['일', '월', '화', '수', '목', '금', '토']

interface LedgerCalendarProps {
  yearMonth: YearMonth
  days: DailyTransactions[]
  selectedDate: string | null
  onSelectDate: (date: string) => void
}

// 한 달 달력. 칸마다 그날 수입/지출을 작게 보여주고, 누르면 그 날짜를 고른다.
// 그날 거래 목록은 부모(TransactionsPage)가 달력 아래에 보여준다.
function LedgerCalendar({ yearMonth, days, selectedDate, onSelectDate }: LedgerCalendarProps) {
  // 날짜로 그날 합계를 바로 찾기 위해 Map으로 바꿔둔다 (배열에서 매번 find하지 않게)
  // new Map([[키, 값], ...]): [키, 값] 쌍의 배열로 Map을 만든다
  const dayMap = new Map(days.map((d) => [d.date, d]))
  const today = getToday()

  // 1일 앞의 빈칸(null) + 1 ~ 말일. 7칸씩 줄바꿈은 grid-cols-7이 알아서 한다.
  const cells: (number | null)[] = [
    ...Array.from({ length: getFirstWeekday(yearMonth) }, () => null),
    ...Array.from({ length: getDaysInMonth(yearMonth) }, (_, i) => i + 1),
  ]

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <div className="grid grid-cols-7 border-b bg-muted/50 text-center text-xs font-medium text-muted-foreground">
        {WEEKDAYS.map((w) => (
          <div key={w} className="py-2">
            {w}
          </div>
        ))}
      </div>
      {/* [&>*]: 바로 아래 자식 모두에 적용. 칸마다 오른쪽·아래 선을 그어 격자를 만든다 */}
      <div className="grid grid-cols-7 [&>*]:border-r [&>*]:border-b [&>*:nth-child(7n)]:border-r-0">
        {cells.map((day, index) => {
          if (day === null) return <div key={`empty-${index}`} />

          const date = toDateKey(yearMonth, day)
          const summary = dayMap.get(date)
          const isSelected = date === selectedDate

          return (
            <button
              key={date}
              type="button"
              // aria-pressed: 선택된 날짜인지 화면낭독기에 알린다.
              // aria-label: 칸 안의 글자("7 3,800,000") 대신 "10월 7일 수입 3,800,000원"처럼 읽어준다
              aria-pressed={isSelected}
              aria-label={[
                `${yearMonth.month}월 ${day}일`,
                summary && summary.income > 0 && `수입 ${formatMoney(summary.income)}`,
                summary && summary.expense > 0 && `지출 ${formatMoney(summary.expense)}`,
              ]
                .filter(Boolean)
                .join(' ')}
              onClick={() => onSelectDate(date)}
              className={cn(
                // min-w-0: 금액이 길어도 칸이 넓어지지 않게 (넘치는 글자는 truncate로 자른다)
                'flex min-h-16 min-w-0 flex-col items-stretch gap-0.5 p-1 text-left hover:bg-muted/50 sm:min-h-20 sm:p-1.5',
                // 선택한 칸은 안쪽 테두리로 표시한다 (배경색만으로는 다크 모드에서 구분이 약했다)
                isSelected && 'bg-accent ring-2 ring-primary ring-inset hover:bg-accent',
              )}
            >
              <span
                className={cn(
                  'text-xs',
                  // 오늘은 굵게 + 밑줄
                  date === today && 'font-bold underline underline-offset-2',
                )}
              >
                {day}
              </span>
              {/* 0원인 쪽은 생략 */}
              {summary !== undefined && summary.income > 0 && (
                <CellAmount amount={summary.income} className="text-income" />
              )}
              {summary !== undefined && summary.expense > 0 && (
                <CellAmount amount={summary.expense} className="text-expense" />
              )}
            </button>
          )
        })}
      </div>
    </div>
  )
}

// 칸 안의 금액. 모바일 칸(약 50px)에는 3,800,000이 들어가지 않아서 '380만'처럼 짧게,
// sm(640px) 이상에서는 원 없이 전체 숫자로 보여준다.
// sm:hidden / hidden sm:inline: 화면 폭에 따라 둘 중 하나만 보이게 한다 (정확한 금액은 아래 목록과 aria-label에 있다)
function CellAmount({ amount, className }: { amount: number; className: string }) {
  return (
    <span className={cn('truncate text-[10px] tabular-nums sm:text-xs', className)}>
      <span className="sm:hidden">{formatMoneyCompact(amount)}</span>
      <span className="hidden sm:inline">{formatAmount(amount)}</span>
    </span>
  )
}

export default LedgerCalendar
