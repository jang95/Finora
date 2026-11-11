import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import MoneyTooltip from '@/components/statistics/MoneyTooltip'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer, type ChartConfig } from '@/components/ui/chart'
import { dailyTotalsQueryKey, getDailyTotals } from '@/services/statisticsService'
import type { DailyTotal } from '@/types/statistics'
import { formatDate, formatMoneyCompact } from '@/utils/format'
import { formatPeriod } from '@/utils/period'

const chartConfig = {
  expense: { label: '지출', color: 'var(--chart-expense)' },
} satisfies ChartConfig

// '2026-10-07' → '10/7' (가로축용. 짧아야 날짜가 많아도 겹치지 않는다)
function shortDate(date: string): string {
  const [, month, day] = date.split('-').map(Number)
  return `${month}/${day}`
}

// 위젯 ④ 일별 지출 추이: 기간 안의 하루하루 지출을 영역 그래프로. 어느 날 많이 썼는지 한눈에 본다.
function DailyExpenseWidget({ period }: WidgetProps) {
  // 하루짜리 기간은 점이 하나라 추이가 없다. 이때는 불러오지 않는다 (enabled: false → 요청 자체를 안 함)
  const isSingleDay = period.from === period.to
  const dailyQuery = useQuery({
    queryKey: dailyTotalsQueryKey(period.from, period.to),
    queryFn: () => getDailyTotals(period.from, period.to),
    placeholderData: keepPreviousData,
    enabled: !isSingleDay,
  })
  const days = dailyQuery.data

  return (
    <WidgetCard
      title="일별 지출 추이"
      description={formatPeriod(period)}
      isStale={dailyQuery.isPlaceholderData}
      error={dailyQuery.error}
      isLoading={!isSingleDay && days === undefined}
    >
      {isSingleDay ? (
        <p className="text-muted-foreground">하루 기간에는 추이가 없습니다. 주간 이상을 선택하세요.</p>
      ) : (
        days && <DailyExpenseChart days={days} />
      )}
    </WidgetCard>
  )
}

function DailyExpenseChart({ days }: { days: DailyTotal[] }) {
  return (
    <>
      <ChartContainer config={chartConfig} className="aspect-auto h-56 w-full">
        {/* AreaChart: 선 아래를 옅게 칠한 그래프. 한 가지 값의 흐름을 볼 때 쓴다 (값이 두 개 이상이면 선만 긋는 LineChart) */}
        <AreaChart data={days} margin={{ left: 0, right: 8 }} accessibilityLayer>
          <CartesianGrid vertical={false} />
          {/* minTickGap: 눈금 글자 사이 최소 간격(px). 날짜가 많으면 Recharts가 알아서 일부만 보여준다 */}
          <XAxis dataKey="date" tickFormatter={shortDate} tickLine={false} axisLine={false} minTickGap={24} />
          <YAxis tickFormatter={formatMoneyCompact} tickLine={false} axisLine={false} width={48} />
          {/* 툴팁 제목은 '10월 7일 (수)'처럼 길게 */}
          <MoneyTooltip config={chartConfig} labelFormatter={(label) => formatDate(String(label))} />
          {/* 선 2px + 아래 영역은 같은 색을 10%만 (영역이 진하면 선이 묻힌다).
              type="monotone": 점 사이를 부드러운 곡선으로 잇되 값을 넘어가지 않게 한다.
              dot={false}: 점이 많아서 평소엔 점을 숨기고, 마우스를 올린 곳만 activeDot으로 보여준다 */}
          <Area
            dataKey="expense"
            type="monotone"
            stroke="var(--color-expense)"
            strokeWidth={2}
            fill="var(--color-expense)"
            fillOpacity={0.1}
            dot={false}
            activeDot={{ r: 4, strokeWidth: 2, stroke: 'var(--background)' }}
          />
        </AreaChart>
      </ChartContainer>

      <details className="text-sm">
        <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
        <table className="mt-2 w-full tabular-nums">
          <thead className="text-muted-foreground">
            <tr>
              <th className="py-1 text-left font-medium">날짜</th>
              <th className="py-1 text-right font-medium">지출</th>
              <th className="py-1 text-right font-medium">누적</th>
            </tr>
          </thead>
          <tbody>
            {days.map((d) => (
              <tr key={d.date} className="border-t">
                <td className="py-1">{formatDate(d.date)}</td>
                <td className="py-1 text-right">
                  <MoneyText amount={d.expense} tone="expense" />
                </td>
                <td className="py-1 text-right">
                  <MoneyText amount={d.cumulativeExpense} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </>
  )
}

export default DailyExpenseWidget
