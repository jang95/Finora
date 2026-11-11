import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import MoneyTooltip from '@/components/statistics/MoneyTooltip'
import WidgetCard from '@/components/statistics/WidgetCard'
import { buildSeriesConfig, seriesKey } from '@/components/statistics/categorySeries'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer, ChartLegend, ChartLegendContent } from '@/components/ui/chart'
import { categoryMonthlyQueryKey, getCategoryMonthly } from '@/services/statisticsService'
import type { CategoryMonthly } from '@/types/statistics'
import { formatMoneyCompact } from '@/utils/format'
import { formatPeriod, getMonthlyChartRange } from '@/utils/period'

// 위젯 ⑦ 상위 카테고리 월별 추이: 달마다 지출을 대분류 상위 5개 + 기타로 쌓은 막대.
// "어느 카테고리가 늘고 있나"를 본다. 범위는 ② 월별 수입·지출과 같다 (기간이 끝나는 달까지 6개월, 연간은 12개월).
function CategoryMonthlyWidget({ period }: WidgetProps) {
  const range = getMonthlyChartRange(period)
  const monthlyQuery = useQuery({
    queryKey: categoryMonthlyQueryKey(range.from, range.to),
    queryFn: () => getCategoryMonthly(range.from, range.to),
    placeholderData: keepPreviousData,
  })
  const data = monthlyQuery.data

  return (
    <WidgetCard
      title="상위 카테고리 월별 추이"
      description={period.unit === 'year' ? formatPeriod(period) : `${data?.months.length ?? 6}개월, 대분류 상위 5개 + 기타`}
      isStale={monthlyQuery.isPlaceholderData}
      error={monthlyQuery.error}
      isLoading={data === undefined}
    >
      {data && (data.series.length === 0 ? <p className="text-muted-foreground">지출 내역이 없습니다.</p> : <StackedChart data={data} />)}
    </WidgetCard>
  )
}

function StackedChart({ data }: { data: CategoryMonthly }) {
  const config = buildSeriesConfig(data.series)
  // Recharts는 한 달 = 객체 하나 { label: '10월', s0: 식비 금액, s1: 교통 금액, ... } 모양을 원한다.
  // amounts 배열을 s0, s1 키로 펼친다. Object.fromEntries 설명은 categorySeries.ts 참고
  const chartData = data.months.map((m) => ({
    label: m.month === 1 ? `${m.year % 100}년 1월` : `${m.month}월`,
    year: m.year,
    month: m.month,
    ...Object.fromEntries(m.amounts.map((amount, i) => [seriesKey(i), amount])),
  }))

  return (
    <>
      <ChartContainer config={config} className="aspect-auto h-64 w-full">
        <BarChart data={chartData} barSize={24} accessibilityLayer>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} />
          <YAxis tickFormatter={formatMoneyCompact} tickLine={false} axisLine={false} width={48} />
          <MoneyTooltip config={config} />
          <ChartLegend content={<ChartLegendContent />} itemSorter={null} />
          {/* stackId가 같은 Bar들은 한 막대에 차곡차곡 쌓인다.
              stroke를 카드 배경색으로 1px 두르면 쌓인 조각 사이에 틈이 생겨 비슷한 색끼리도 구분된다 */}
          {data.series.map((_, i) => (
            <Bar
              key={seriesKey(i)}
              dataKey={seriesKey(i)}
              stackId="expense"
              fill={`var(--color-${seriesKey(i)})`}
              stroke="var(--card)"
              strokeWidth={1}
              // 맨 위 조각만 모서리를 둥글게 (막대 끝 표시)
              radius={i === data.series.length - 1 ? [4, 4, 0, 0] : 0}
            />
          ))}
        </BarChart>
      </ChartContainer>

      {/* 색 대비가 낮은 색(청록·노랑·분홍)이 있어서 표는 꼭 둔다 (index.css chart 토큰 설명) */}
      <details className="text-sm">
        <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
        {/* 열이 많아서 좁은 화면에서는 가로로 스크롤 */}
        <div className="mt-2 overflow-x-auto">
          <table className="w-full tabular-nums">
            <thead className="text-muted-foreground">
              <tr>
                <th className="py-1 text-left font-medium">월</th>
                {data.series.map((s, i) => (
                  <th key={seriesKey(i)} className="py-1 pl-3 text-right font-medium whitespace-nowrap">
                    {s.name}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.months.map((m) => (
                <tr key={`${m.year}-${m.month}`} className="border-t">
                  <td className="py-1 whitespace-nowrap">
                    {m.year}년 {m.month}월
                  </td>
                  {m.amounts.map((amount, i) => (
                    <td key={seriesKey(i)} className="py-1 pl-3 text-right">
                      <MoneyText amount={amount} />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </details>
    </>
  )
}

export default CategoryMonthlyWidget
