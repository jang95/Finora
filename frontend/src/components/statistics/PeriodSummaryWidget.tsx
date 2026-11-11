import { keepPreviousData, useQuery } from '@tanstack/react-query'
import LedgerSummary from '@/components/transaction/LedgerSummary'
import { cn } from '@/lib/utils'
import { getPeriodSummary, periodSummaryQueryKey } from '@/services/statisticsService'
import type { WidgetProps } from '@/components/statistics/widgets'

// 위젯 ① 기간 요약: 수입 / 지출 / 합계.
// [위젯이 직접 useQuery를 쓰는 이유] 대시보드는 위젯을 숨기거나 순서를 바꾸므로, 화면(page)이 모든 데이터를 모아서
//   props로 내려주면 숨긴 위젯 데이터까지 불러오게 된다. 위젯이 각자 service를 부르면 보이는 것만 불러온다. (결정 D26)
//   fetch를 직접 쓰지 않고 services/를 거치는 규칙은 그대로다.
function PeriodSummaryWidget({ period }: WidgetProps) {
  // [useQuery] 설명은 lib/queryClient.ts, DashboardPage 참고. 이름표에 from, to가 있어서 기간을 바꾸면 새로 불러온다.
  const summaryQuery = useQuery({
    queryKey: periodSummaryQueryKey(period.from, period.to),
    queryFn: () => getPeriodSummary(period.from, period.to),
    placeholderData: keepPreviousData,
  })
  const summary = summaryQuery.data

  if (summaryQuery.error) {
    return (
      <p role="alert" className="text-destructive">
        {summaryQuery.error.message}
      </p>
    )
  }
  if (summary === undefined) {
    return <p className="text-muted-foreground">불러오는 중...</p>
  }

  return (
    <div className={cn(summaryQuery.isPlaceholderData && 'opacity-60')}>
      <LedgerSummary income={summary.income} expense={summary.expense} balance={summary.balance} />
    </div>
  )
}

export default PeriodSummaryWidget
