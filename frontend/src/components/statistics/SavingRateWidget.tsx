import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { CartesianGrid, Line, LineChart, ReferenceLine, XAxis, YAxis } from 'recharts'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from '@/components/ui/chart'
import { getMonthlyTotals, monthlyTotalsQueryKey } from '@/services/statisticsService'
import type { MonthlyTotal } from '@/types/statistics'
import { formatRatio } from '@/utils/format'
import { formatPeriod, getMonthlyChartRange } from '@/utils/period'

// 저축률은 수입·지출 어느 쪽도 아닌 "비율"이라 수입색(초록)을 쓴다: 높을수록 좋은 값
const chartConfig = {
  savingRate: { label: '저축률', color: 'var(--chart-income)' },
} satisfies ChartConfig

// 위젯 ⑩ 저축률 추이: 달마다 (수입 − 지출) ÷ 수입. 범위는 ② 월별 수입·지출과 같다 (같은 API, 같은 캐시).
function SavingRateWidget({ period }: WidgetProps) {
  const range = getMonthlyChartRange(period)
  const monthlyQuery = useQuery({
    queryKey: monthlyTotalsQueryKey(range.from, range.to),
    queryFn: () => getMonthlyTotals(range.from, range.to),
    placeholderData: keepPreviousData,
  })
  const totals = monthlyQuery.data

  return (
    <WidgetCard
      title="저축률 추이"
      description={period.unit === 'year' ? formatPeriod(period) : `${totals?.length ?? 6}개월, (수입 − 지출) ÷ 수입`}
      isStale={monthlyQuery.isPlaceholderData}
      error={monthlyQuery.error}
      isLoading={totals === undefined}
    >
      {totals && <SavingRateChart totals={totals} />}
    </WidgetCard>
  )
}

function SavingRateChart({ totals }: { totals: MonthlyTotal[] }) {
  const chartData = totals.map((t) => ({
    ...t,
    label: t.month === 1 ? `${t.year % 100}년 1월` : `${t.month}월`,
  }))

  return (
    <>
      <ChartContainer config={chartConfig} className="aspect-auto h-56 w-full">
        <LineChart data={chartData} margin={{ left: 0, right: 8 }} accessibilityLayer>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} />
          {/* 축은 %. 지출이 수입보다 크면 음수가 된다 */}
          <YAxis tickFormatter={(v) => `${v}%`} tickLine={false} axisLine={false} width={44} />
          {/* 0% 기준선: 이 아래면 그달은 번 것보다 더 썼다. 데이터가 아닌 보조선이라 점선·연한 색 */}
          <ReferenceLine y={0} stroke="var(--muted-foreground)" strokeDasharray="3 3" />
          <ChartTooltip
            content={
              <ChartTooltipContent
                formatter={(value) => (
                  <div className="flex w-full items-center gap-2">
                    <span className="size-2.5 shrink-0 rounded-[2px] bg-chart-income" />
                    <span className="text-muted-foreground">{chartConfig.savingRate.label}</span>
                    <span className="ml-auto font-medium tabular-nums">{formatRatio(Number(value))}</span>
                  </div>
                )}
              />
            }
          />
          {/* connectNulls={false}: 수입이 0이라 저축률이 없는 달(null)은 선을 끊는다 (0%로 그리면 거짓말이 된다) */}
          <Line dataKey="savingRate" type="monotone" stroke="var(--color-savingRate)" strokeWidth={2} dot={{ r: 4 }} activeDot={{ r: 5 }} connectNulls={false} />
        </LineChart>
      </ChartContainer>

      <details className="text-sm">
        <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
        <table className="mt-2 w-full tabular-nums">
          <thead className="text-muted-foreground">
            <tr>
              <th className="py-1 text-left font-medium">월</th>
              <th className="py-1 text-right font-medium">저축률</th>
            </tr>
          </thead>
          <tbody>
            {totals.map((t) => (
              <tr key={`${t.year}-${t.month}`} className="border-t">
                <td className="py-1">
                  {t.year}년 {t.month}월
                </td>
                <td className="py-1 text-right">{t.savingRate === null ? <span className="text-muted-foreground">수입 없음</span> : formatRatio(t.savingRate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </>
  )
}

export default SavingRateWidget
