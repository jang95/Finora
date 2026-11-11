import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Cell, Label, Pie, PieChart } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import MoneyTooltip from '@/components/statistics/MoneyTooltip'
import WidgetCard from '@/components/statistics/WidgetCard'
import { buildSeriesConfig, seriesColor, seriesKey } from '@/components/statistics/categorySeries'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer } from '@/components/ui/chart'
import { categorySharesQueryKey, getCategoryShares } from '@/services/statisticsService'
import type { CategoryShares } from '@/types/statistics'
import { formatMoneyCompact, formatRatio } from '@/utils/format'
import { formatPeriod } from '@/utils/period'

// 위젯 ⑥ 카테고리 비중 도넛: 기간 지출을 대분류 상위 5개 + 기타로. 가운데에 지출 합계.
// 원그래프는 조각끼리 크기 비교가 어렵지만 "전체 중 얼마"를 한눈에 보는 데는 좋다. 정확한 비교는 ③ 카테고리별 지출 막대가 맡는다.
function CategoryDonutWidget({ period }: WidgetProps) {
  const sharesQuery = useQuery({
    queryKey: categorySharesQueryKey(period.from, period.to),
    queryFn: () => getCategoryShares(period.from, period.to),
    placeholderData: keepPreviousData,
  })
  const shares = sharesQuery.data

  return (
    <WidgetCard
      title="카테고리 비중"
      description={`${formatPeriod(period)} 지출, 상위 5개 + 기타`}
      isStale={sharesQuery.isPlaceholderData}
      error={sharesQuery.error}
      isLoading={shares === undefined}
    >
      {shares && (shares.items.length === 0 ? <p className="text-muted-foreground">지출 내역이 없습니다.</p> : <DonutChart shares={shares} />)}
    </WidgetCard>
  )
}

function DonutChart({ shares }: { shares: CategoryShares }) {
  // 조각마다 짧은 키(s0, s1 ...)를 붙인다. 툴팁·색이 이 키로 config를 찾는다
  const chartData = shares.items.map((item, i) => ({ ...item, key: seriesKey(i) }))
  const config = buildSeriesConfig(shares.items)

  return (
    // 좁은 폭(모바일·반 칸)에서는 도넛 아래에 목록, 넓으면 옆에 목록
    <div className="grid items-center gap-4 sm:grid-cols-[auto_1fr]">
      <ChartContainer config={config} className="mx-auto aspect-square h-48">
        <PieChart accessibilityLayer>
          <MoneyTooltip config={config} />
          {/* innerRadius로 가운데를 비워 도넛을 만든다. paddingAngle: 조각 사이 틈(배경색이 보여서 조각이 구분된다).
              dataKey: 조각 크기로 쓸 값, nameKey: 툴팁이 이름표를 찾을 키 */}
          <Pie data={chartData} dataKey="amount" nameKey="key" innerRadius={55} outerRadius={85} paddingAngle={2} strokeWidth={0}>
            {/* Cell: 조각 하나의 색. Pie 안에 데이터 개수만큼 둔다 */}
            {chartData.map((item, i) => (
              <Cell key={item.key} fill={seriesColor(i, item.categoryId === null)} />
            ))}
            {/* 가운데 글자. viewBox에서 중심(cx, cy)을 받아 그 자리에 SVG <text>를 그린다 */}
            <Label
              content={({ viewBox }) => {
                if (!viewBox || !('cx' in viewBox) || viewBox.cx === undefined || viewBox.cy === undefined) return null
                return (
                  <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle" dominantBaseline="middle">
                    {/* tspan: 한 <text> 안에서 줄을 나눈다. fill-foreground: 글자색은 데이터 색이 아니라 글자 토큰 */}
                    <tspan x={viewBox.cx} y={viewBox.cy - 8} className="fill-foreground text-lg font-bold">
                      {formatMoneyCompact(shares.total)}
                    </tspan>
                    <tspan x={viewBox.cx} y={viewBox.cy + 14} className="fill-muted-foreground text-xs">
                      지출 합계
                    </tspan>
                  </text>
                )
              }}
            />
          </Pie>
        </PieChart>
      </ChartContainer>

      {/* 범례 겸 표: 색만으로 구분하지 않도록 이름·비율·금액을 글자로 함께 보여준다 */}
      <ul className="flex flex-col gap-1.5 text-sm">
        {chartData.map((item, i) => (
          <li key={item.key} className="flex items-center gap-2">
            <span className="size-2.5 shrink-0 rounded-[2px]" style={{ backgroundColor: seriesColor(i, item.categoryId === null) }} />
            <span className="truncate">{item.name}</span>
            <span className="ml-auto whitespace-nowrap tabular-nums">
              <span className="text-muted-foreground">{formatRatio(item.ratio)}</span>
              <MoneyText amount={item.amount} className="ml-2 font-medium" />
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

export default CategoryDonutWidget
