import { keepPreviousData, useQuery } from '@tanstack/react-query'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import TransactionRow from '@/components/transaction/TransactionRow'
import { TOP_EXPENSE_COUNT, getTopExpenses, topExpensesQueryKey } from '@/services/statisticsService'
import { formatPeriod } from '@/utils/period'

// 위젯 ⑨ 큰 지출 TOP 5: 기간 안에서 금액이 큰 거래 다섯 건. 그래프가 아니라 목록이다 (어떤 거래였는지가 중요하므로).
// 거래 한 줄은 가계부·대시보드와 같은 TransactionRow를 쓴다. 누르면 수정하는 기능은 가계부 화면에만 두고 여기서는 보기만 한다.
function TopExpensesWidget({ period }: WidgetProps) {
  const topQuery = useQuery({
    queryKey: topExpensesQueryKey(period.from, period.to),
    queryFn: () => getTopExpenses(period.from, period.to),
    placeholderData: keepPreviousData,
  })
  const items = topQuery.data

  return (
    <WidgetCard
      title={`큰 지출 TOP ${TOP_EXPENSE_COUNT}`}
      description={formatPeriod(period)}
      isStale={topQuery.isPlaceholderData}
      error={topQuery.error}
      isLoading={items === undefined}
    >
      {items &&
        (items.length === 0 ? (
          <p className="text-muted-foreground">지출 내역이 없습니다.</p>
        ) : (
          // -mx-4: 카드 안쪽 여백을 상쇄해서 TransactionRow의 자체 여백(px-4)과 줄이 카드 가장자리까지 닿게 한다
          <ol className="-mx-4 divide-y">
            {items.map((t, i) => (
              <li key={t.id} className="flex items-center">
                {/* 순위. 글자색은 데이터 색이 아니라 글자 토큰 */}
                <span className="w-8 shrink-0 pl-4 text-sm font-semibold text-muted-foreground tabular-nums">{i + 1}</span>
                <div className="min-w-0 flex-1">
                  <TransactionRow transaction={t} showDate />
                </div>
              </li>
            ))}
          </ol>
        ))}
    </WidgetCard>
  )
}

export default TopExpensesWidget
