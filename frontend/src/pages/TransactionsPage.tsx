import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect, useState } from 'react'
import MoneyText from '@/components/common/MoneyText'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { getTransactions } from '@/services/transactionService'
import type { Transaction } from '@/types/transaction'
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

  // 이전 값(prev)을 받아 새 값을 만드는 형태. 버튼을 빠르게 여러 번 눌러도 항상 최신 값 기준으로 계산된다.
  function moveMonth(offset: number) {
    setYearMonth((prev) => addMonths(prev, offset))
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
  }, [year, month])

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
                <TransactionRow transaction={t} />
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  )
}

// 거래 한 줄. 이 화면에서만 쓰므로 같은 파일에 둔다. (다른 화면에서도 쓰게 되면 components/transaction/으로 옮긴다)
function TransactionRow({ transaction: t }: { transaction: Transaction }) {
  const isTransfer = t.type === 'Transfer'

  return (
    <div className="flex items-center justify-between gap-4 px-4 py-3">
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
    </div>
  )
}

export default TransactionsPage
