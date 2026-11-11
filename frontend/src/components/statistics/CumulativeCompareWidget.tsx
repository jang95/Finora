import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { CartesianGrid, Line, LineChart, XAxis, YAxis } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import MoneyTooltip from '@/components/statistics/MoneyTooltip'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer, ChartLegend, ChartLegendContent, type ChartConfig } from '@/components/ui/chart'
import { dailyTotalsQueryKey, getDailyTotals } from '@/services/statisticsService'
import type { DailyTotal } from '@/types/statistics'
import { formatMoneyCompact } from '@/utils/format'
import { formatPeriod, shiftPeriod } from '@/utils/period'

// 이번 기간은 지출색, 이전 기간은 회색(비교 기준이라 눈에 덜 띄게). 회색은 테마의 chart-2 토큰
const chartConfig = {
  current: { label: '이번 기간', color: 'var(--chart-expense)' },
  previous: { label: '이전 기간', color: 'var(--chart-2)' },
} satisfies ChartConfig

// 위젯 ⑤ 이전 기간 누적 비교: 기간 첫날부터의 지출 누적을 이번 기간·이전 기간 두 선으로.
// "이달은 지난달보다 빨리 쓰고 있나?"를 본다. 가로축은 날짜가 아니라 "며칠째"다 (10월 31일, 11월 30일처럼 길이가 달라서).
function CumulativeCompareWidget({ period }: WidgetProps) {
  const isSingleDay = period.from === period.to
  // 이전 기간 = 같은 단위로 한 칸 앞 (월간이면 지난달, 주간이면 지난주)
  const previous = shiftPeriod(period, -1)

  // 같은 API를 기간만 다르게 두 번 부른다. 이름표에 from·to가 있어서 서로 다른 캐시 항목이 된다.
  // 이번 기간 요청은 DailyExpenseWidget과 이름표가 같아서 한 번만 불러오고 둘이 나눠 쓴다
  const currentQuery = useQuery({
    queryKey: dailyTotalsQueryKey(period.from, period.to),
    queryFn: () => getDailyTotals(period.from, period.to),
    placeholderData: keepPreviousData,
    enabled: !isSingleDay,
  })
  const previousQuery = useQuery({
    queryKey: dailyTotalsQueryKey(previous.from, previous.to),
    queryFn: () => getDailyTotals(previous.from, previous.to),
    placeholderData: keepPreviousData,
    enabled: !isSingleDay,
  })

  return (
    <WidgetCard
      title="이전 기간 누적 비교"
      description={`${formatPeriod(period)} vs ${formatPeriod(previous)}`}
      isStale={currentQuery.isPlaceholderData || previousQuery.isPlaceholderData}
      error={currentQuery.error ?? previousQuery.error}
      isLoading={!isSingleDay && (currentQuery.data === undefined || previousQuery.data === undefined)}
    >
      {isSingleDay ? (
        <p className="text-muted-foreground">하루 기간에는 누적이 없습니다. 주간 이상을 선택하세요.</p>
      ) : (
        currentQuery.data && previousQuery.data && <CumulativeChart current={currentQuery.data} previous={previousQuery.data} />
      )}
    </WidgetCard>
  )
}

function CumulativeChart({ current, previous }: { current: DailyTotal[]; previous: DailyTotal[] }) {
  // 두 기간을 "며칠째"로 나란히 놓는다. 짧은 쪽이 끝나면 그 선은 거기서 멈춘다 (값 undefined → Recharts가 선을 끊는다).
  // 누적 금액은 Backend가 계산한 cumulativeExpense를 그대로 쓴다. 여기서는 더하지 않는다 (결정 D4)
  const length = Math.max(current.length, previous.length)
  const chartData = Array.from({ length }, (_, i) => ({
    day: i + 1,
    current: current[i]?.cumulativeExpense,
    previous: previous[i]?.cumulativeExpense,
  }))
  // 기간 전체 누적 = 마지막 날의 누적. at(-1): 마지막 항목
  const currentTotal = current.at(-1)?.cumulativeExpense ?? 0
  const previousTotal = previous.at(-1)?.cumulativeExpense ?? 0

  return (
    <>
      {/* 두 기간의 최종 누적을 숫자로도. 그래프 끝점을 읽지 않아도 된다 */}
      <dl className="flex gap-6 text-sm">
        <div className="flex flex-col">
          <dt className="text-muted-foreground">이번 기간</dt>
          <dd>
            <MoneyText amount={currentTotal} tone="expense" className="font-semibold" />
          </dd>
        </div>
        <div className="flex flex-col">
          <dt className="text-muted-foreground">이전 기간</dt>
          <dd>
            <MoneyText amount={previousTotal} className="font-semibold" />
          </dd>
        </div>
      </dl>

      <ChartContainer config={chartConfig} className="aspect-auto h-56 w-full">
        <LineChart data={chartData} margin={{ left: 0, right: 8 }} accessibilityLayer>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="day" tickFormatter={(day) => `${day}일째`} tickLine={false} axisLine={false} minTickGap={24} />
          <YAxis tickFormatter={formatMoneyCompact} tickLine={false} axisLine={false} width={48} />
          <MoneyTooltip config={chartConfig} labelFormatter={(label) => `${label}일째`} />
          {/* 선이 둘이라 범례가 필요하다 (색만으로 구분하게 두지 않는다) */}
          <ChartLegend content={<ChartLegendContent />} itemSorter={null} />
          <Line dataKey="current" type="monotone" stroke="var(--color-current)" strokeWidth={2} dot={false} activeDot={{ r: 4 }} />
          <Line dataKey="previous" type="monotone" stroke="var(--color-previous)" strokeWidth={2} dot={false} activeDot={{ r: 4 }} />
        </LineChart>
      </ChartContainer>

      <details className="text-sm">
        <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
        <table className="mt-2 w-full tabular-nums">
          <thead className="text-muted-foreground">
            <tr>
              <th className="py-1 text-left font-medium">며칠째</th>
              <th className="py-1 text-right font-medium">이번 기간</th>
              <th className="py-1 text-right font-medium">이전 기간</th>
            </tr>
          </thead>
          <tbody>
            {chartData.map((row) => (
              <tr key={row.day} className="border-t">
                <td className="py-1">{row.day}일째</td>
                <td className="py-1 text-right">{row.current === undefined ? '-' : <MoneyText amount={row.current} />}</td>
                <td className="py-1 text-right">{row.previous === undefined ? '-' : <MoneyText amount={row.previous} />}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </>
  )
}

export default CumulativeCompareWidget
