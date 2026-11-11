import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Area, AreaChart, CartesianGrid, ReferenceLine, XAxis, YAxis } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import MoneyTooltip from '@/components/statistics/MoneyTooltip'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer, type ChartConfig } from '@/components/ui/chart'
import { getMonthlyTotals, monthlyTotalsQueryKey } from '@/services/statisticsService'
import type { MonthlyTotal } from '@/types/statistics'
import { formatMoneyCompact } from '@/utils/format'
import { formatPeriod, getMonthlyChartRange } from '@/utils/period'

// 총자산은 수입/지출이 아니라 "쌓인 돈"이라 구분 색 1번(파랑)을 쓴다
const chartConfig = {
  totalAssets: { label: '총자산', color: 'var(--chart-1)' },
} satisfies ChartConfig

// 위젯 ⑪ 총자산 추이 (가계부 기준): 달마다 그달 말까지의 누적 수입 − 지출 (결정 D17). 계좌 기준 총자산은 Phase 8.
// 범위는 ② 월별 수입·지출과 같다 (같은 API, 같은 캐시).
function TotalAssetsWidget({ period }: WidgetProps) {
  const range = getMonthlyChartRange(period)
  const monthlyQuery = useQuery({
    queryKey: monthlyTotalsQueryKey(range.from, range.to),
    queryFn: () => getMonthlyTotals(range.from, range.to),
    placeholderData: keepPreviousData,
  })
  const totals = monthlyQuery.data

  return (
    <WidgetCard
      title="총자산 추이"
      description={`${period.unit === 'year' ? formatPeriod(period) : `${totals?.length ?? 6}개월`}, 가계부 기준 (월말 누적)`}
      isStale={monthlyQuery.isPlaceholderData}
      error={monthlyQuery.error}
      isLoading={totals === undefined}
    >
      {totals && <TotalAssetsChart totals={totals} />}
    </WidgetCard>
  )
}

function TotalAssetsChart({ totals }: { totals: MonthlyTotal[] }) {
  const chartData = totals.map((t) => ({
    ...t,
    label: t.month === 1 ? `${t.year % 100}년 1월` : `${t.month}월`,
  }))

  return (
    <>
      <ChartContainer config={chartConfig} className="aspect-auto h-56 w-full">
        <AreaChart data={chartData} margin={{ left: 0, right: 8 }} accessibilityLayer>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} />
          <YAxis tickFormatter={formatMoneyCompact} tickLine={false} axisLine={false} width={52} />
          {/* 누적이 음수(지출이 수입보다 많이 쌓임)일 수 있어서 0 기준선을 둔다 */}
          <ReferenceLine y={0} stroke="var(--muted-foreground)" strokeDasharray="3 3" />
          <MoneyTooltip config={chartConfig} />
          <Area
            dataKey="totalAssets"
            type="monotone"
            stroke="var(--color-totalAssets)"
            strokeWidth={2}
            fill="var(--color-totalAssets)"
            fillOpacity={0.1}
            dot={{ r: 4 }}
            activeDot={{ r: 5 }}
          />
        </AreaChart>
      </ChartContainer>

      <details className="text-sm">
        <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
        <table className="mt-2 w-full tabular-nums">
          <thead className="text-muted-foreground">
            <tr>
              <th className="py-1 text-left font-medium">월</th>
              <th className="py-1 text-right font-medium">월말 총자산</th>
            </tr>
          </thead>
          <tbody>
            {totals.map((t) => (
              <tr key={`${t.year}-${t.month}`} className="border-t">
                <td className="py-1">
                  {t.year}년 {t.month}월
                </td>
                <td className="py-1 text-right">
                  <MoneyText amount={t.totalAssets} signed />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </>
  )
}

export default TotalAssetsWidget
