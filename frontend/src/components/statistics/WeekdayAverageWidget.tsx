import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import MoneyTooltip from '@/components/statistics/MoneyTooltip'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import { ChartContainer, type ChartConfig } from '@/components/ui/chart'
import { getWeekdayAverages, weekdayAveragesQueryKey } from '@/services/statisticsService'
import type { Weekday, WeekdayAverage } from '@/types/statistics'
import { formatMoneyCompact } from '@/utils/format'
import { formatPeriod } from '@/utils/period'

const chartConfig = {
  average: { label: '하루 평균', color: 'var(--chart-expense)' },
} satisfies ChartConfig

// Backend의 DayOfWeek 이름 → 한글 한 글자. Record<Weekday, string>: 7개 요일을 모두 적어야 컴파일된다
const WEEKDAY_LABELS: Record<Weekday, string> = {
  Monday: '월',
  Tuesday: '화',
  Wednesday: '수',
  Thursday: '목',
  Friday: '금',
  Saturday: '토',
  Sunday: '일',
}

// 위젯 ⑧ 요일별 평균 지출: "주말에 많이 쓰나?"를 본다. 평균 = 요일 지출 합계 ÷ 기간 안 그 요일의 날 수 (안 쓴 날도 포함).
function WeekdayAverageWidget({ period }: WidgetProps) {
  const weekdayQuery = useQuery({
    queryKey: weekdayAveragesQueryKey(period.from, period.to),
    queryFn: () => getWeekdayAverages(period.from, period.to),
    placeholderData: keepPreviousData,
  })
  const weekdays = weekdayQuery.data

  return (
    <WidgetCard
      title="요일별 평균 지출"
      description={`${formatPeriod(period)}, 하루 평균`}
      isStale={weekdayQuery.isPlaceholderData}
      error={weekdayQuery.error}
      isLoading={weekdays === undefined}
    >
      {weekdays && <WeekdayChart weekdays={weekdays} />}
    </WidgetCard>
  )
}

function WeekdayChart({ weekdays }: { weekdays: WeekdayAverage[] }) {
  const chartData = weekdays.map((w) => ({ ...w, label: WEEKDAY_LABELS[w.weekday] }))

  return (
    <>
      <ChartContainer config={chartConfig} className="aspect-auto h-48 w-full">
        <BarChart data={chartData} barSize={24} accessibilityLayer>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} />
          <YAxis tickFormatter={formatMoneyCompact} tickLine={false} axisLine={false} width={48} />
          {/* 툴팁 제목: '수요일 (2일 평균)' */}
          <MoneyTooltip
            config={chartConfig}
            labelFormatter={(label) => {
              const row = chartData.find((w) => w.label === label)
              return row ? `${label}요일 (${row.dayCount}일 평균)` : String(label)
            }}
          />
          <Bar dataKey="average" fill="var(--color-average)" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ChartContainer>

      <details className="text-sm">
        <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
        <table className="mt-2 w-full tabular-nums">
          <thead className="text-muted-foreground">
            <tr>
              <th className="py-1 text-left font-medium">요일</th>
              <th className="py-1 text-right font-medium">날 수</th>
              <th className="py-1 text-right font-medium">합계</th>
              <th className="py-1 text-right font-medium">평균</th>
            </tr>
          </thead>
          <tbody>
            {chartData.map((w) => (
              <tr key={w.weekday} className="border-t">
                <td className="py-1">{w.label}</td>
                <td className="py-1 text-right">{w.dayCount}일</td>
                <td className="py-1 text-right">
                  <MoneyText amount={w.total} />
                </td>
                <td className="py-1 text-right">
                  <MoneyText amount={w.average} tone="expense" />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </>
  )
}

export default WeekdayAverageWidget
