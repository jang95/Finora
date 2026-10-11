import MoneyText from '@/components/common/MoneyText'
import TransactionRow from '@/components/transaction/TransactionRow'
import { Card } from '@/components/ui/card'
import type { DailyTransactions, Transaction } from '@/types/transaction'
import { formatDate } from '@/utils/format'

interface DailyTransactionListProps {
  days: DailyTransactions[]
  onSelect: (transaction: Transaction) => void
}

// 날짜별로 묶은 거래 목록. 날짜 제목 줄에 그날 수입/지출 소계를 붙인다.
function DailyTransactionList({ days, onSelect }: DailyTransactionListProps) {
  if (days.length === 0) {
    return <p className="text-muted-foreground">거래 내역이 없습니다.</p>
  }

  return (
    <Card className="gap-0 py-0">
      {/* divide-y: 항목 "사이"에만 선을 긋는다 (마지막 항목 아래는 선 없음) */}
      <ul className="divide-y">
        {/* map: 배열의 각 항목을 화면 요소로 바꾼다. C# LINQ의 Select와 같다.
            key: React가 목록에서 어떤 항목이 추가·삭제·이동됐는지 구분하는 값. 항목마다 고유해야 한다.
              날짜 묶음은 하루에 하나이므로 날짜를, 거래는 id를 쓴다. */}
        {days.map((day) => (
          <li key={day.date}>
            <DayHeader day={day} />
            <ul className="divide-y">
              {day.transactions.map((t) => (
                <li key={t.id}>
                  <TransactionRow transaction={t} onClick={() => onSelect(t)} />
                </li>
              ))}
            </ul>
          </li>
        ))}
      </ul>
    </Card>
  )
}

// 날짜 제목 줄: "10월 7일 (수)"  ···  +수입  -지출 (0원인 쪽은 생략)
function DayHeader({ day }: { day: DailyTransactions }) {
  return (
    <div className="flex items-center justify-between gap-4 border-b bg-muted/50 px-4 py-2 text-xs font-medium">
      <span className="text-muted-foreground">{formatDate(day.date)}</span>
      <span className="flex gap-3">
        {day.income > 0 && <MoneyText amount={day.income} tone="income" />}
        {day.expense > 0 && <MoneyText amount={day.expense} tone="expense" />}
      </span>
    </div>
  )
}

export default DailyTransactionList
