import { ChevronRight } from 'lucide-react'
import MoneyText from '@/components/common/MoneyText'
import { Card } from '@/components/ui/card'
import type { MonthlyBalance } from '@/types/transaction'

interface MonthlyBalanceTableProps {
  months: MonthlyBalance[]
  // 달을 누르면 부모가 그 달의 목록 탭으로 이동한다
  onSelectMonth: (month: number) => void
}

// 한 해의 월별 수입/지출/합계 표. 행을 누르면 그 달로 내려간다(드릴다운).
function MonthlyBalanceTable({ months, onSelectMonth }: MonthlyBalanceTableProps) {
  return (
    <Card className="gap-0 py-0">
      {/* 표 대신 목록 + grid로 만든다. 행 전체를 button으로 만들어야 키보드로도 누를 수 있는데,
          <tr>은 button이 될 수 없기 때문. grid-cols-[...]: 열 너비를 직접 정한다 (월 칸은 좁게, 금액 3칸은 같은 비율) */}
      <div className="grid grid-cols-[3rem_1fr_1fr_1fr_1rem] gap-2 border-b bg-muted/50 px-4 py-2 text-xs font-medium text-muted-foreground">
        <span>월</span>
        <span className="text-right">수입</span>
        <span className="text-right">지출</span>
        <span className="text-right">합계</span>
      </div>
      <ul className="divide-y">
        {months.map((m) => {
          const isEmpty = m.income === 0 && m.expense === 0
          return (
            <li key={m.month}>
              <button
                type="button"
                onClick={() => onSelectMonth(m.month)}
                className="grid w-full grid-cols-[3rem_1fr_1fr_1fr_1rem] items-center gap-2 px-4 py-3 text-left text-xs hover:bg-muted/50 sm:text-sm"
              >
                <span className="font-medium">{m.month}월</span>
                {isEmpty ? (
                  // 거래가 없는 달은 0원을 세 번 늘어놓지 않고 '-' 하나로 대신한다
                  <span className="col-span-3 text-right text-muted-foreground">-</span>
                ) : (
                  <>
                    <MoneyText amount={m.income} tone="income" className="text-right break-all" />
                    <MoneyText amount={m.expense} tone="expense" className="text-right break-all" />
                    <MoneyText amount={m.balance} signed className="text-right break-all" />
                  </>
                )}
                <ChevronRight className="size-4 text-muted-foreground" aria-hidden />
              </button>
            </li>
          )
        })}
      </ul>
    </Card>
  )
}

export default MonthlyBalanceTable
