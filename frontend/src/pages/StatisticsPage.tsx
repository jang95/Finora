import { useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from 'recharts'
import MoneyText from '@/components/common/MoneyText'
import PeriodPicker from '@/components/common/PeriodPicker'
import LedgerSummary from '@/components/transaction/LedgerSummary'
import { cn } from '@/lib/utils'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import {
  categoryExpensesQueryKey,
  getCategoryExpenses,
  getMonthlyTotals,
  getPeriodSummary,
  monthlyTotalsQueryKey,
  periodSummaryQueryKey,
} from '@/services/statisticsService'
import type { CategoryExpense, MonthlyTotal } from '@/types/statistics'
import { formatMoneyCompact, formatRatio } from '@/utils/format'
import { formatPeriod, getMonthlyChartRange, getPeriod } from '@/utils/period'
import { getToday } from '@/utils/yearMonth'

// shadcn 차트 설정: 데이터 키(income/expense)마다 범례·툴팁에 쓸 이름과 색을 정한다.
// 여기 적은 색은 차트 안에서 --color-income, --color-expense 변수로 쓸 수 있다 (아래 <Bar fill=...>).
// 색은 차트 전용 토큰을 쓴다. 이유는 index.css의 chart-income/expense 설명 참고.
// satisfies: "이 타입 규칙에 맞는지 검사만 하고, 타입은 적은 그대로 유지"한다. 오타가 있으면 에러가 난다.
const chartConfig = {
  income: { label: '수입', color: 'var(--chart-income)' },
  expense: { label: '지출', color: 'var(--chart-expense)' },
} satisfies ChartConfig

function StatisticsPage() {
  // 고른 기간. 처음엔 이번 달. 대시보드와 공유하지 않는다 (화면마다 따로)
  const [period, setPeriod] = useState(() => getPeriod('month', getToday()))
  // 월별 그래프 범위: 기간이 끝나는 달까지 6개월 (연간이면 그해 12개월)
  const chartRange = getMonthlyChartRange(period)

  // [useQuery] 데이터 불러오기 (설명은 lib/queryClient.ts, DashboardPage 참고)
  // 셋 다 이름표가 ['transactions'] 아래라 거래를 바꾸면 함께 다시 불러온다.
  // keepPreviousData: 기간을 바꾸는 동안 이전 내용을 보여줘서 화면이 깜빡이지 않게 한다.
  const summaryQuery = useQuery({
    queryKey: periodSummaryQueryKey(period.from, period.to),
    queryFn: () => getPeriodSummary(period.from, period.to),
    placeholderData: keepPreviousData,
  })
  const categoryQuery = useQuery({
    queryKey: categoryExpensesQueryKey(period.from, period.to),
    queryFn: () => getCategoryExpenses(period.from, period.to),
    placeholderData: keepPreviousData,
  })
  const monthlyQuery = useQuery({
    queryKey: monthlyTotalsQueryKey(chartRange.from, chartRange.to),
    queryFn: () => getMonthlyTotals(chartRange.from, chartRange.to),
    placeholderData: keepPreviousData,
  })

  const summary = summaryQuery.data
  const categoryExpenses = categoryQuery.data
  const monthlyTotals = monthlyQuery.data
  const loadError = summaryQuery.error ?? categoryQuery.error ?? monthlyQuery.error
  // 셋 중 하나라도 이전 기간 데이터를 보여주는 중이면 흐리게
  const isStale = summaryQuery.isPlaceholderData || categoryQuery.isPlaceholderData || monthlyQuery.isPlaceholderData

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">통계</h1>

      <PeriodPicker value={period} onChange={setPeriod} />

      {loadError ? (
        <p role="alert" className="text-destructive">
          {loadError.message}
        </p>
      ) : summary === undefined || categoryExpenses === undefined || monthlyTotals === undefined ? (
        <p className="text-muted-foreground">불러오는 중...</p>
      ) : (
        <div className={cn('flex flex-col gap-6', isStale && 'opacity-60')}>
          <LedgerSummary income={summary.income} expense={summary.expense} balance={summary.balance} />
          <MonthlyChartCard
            totals={monthlyTotals}
            description={period.unit === 'year' ? formatPeriod(period) : `${monthlyTotals.length}개월 (선택한 기간이 끝나는 달까지)`}
          />
          <CategoryExpenseCard expenses={categoryExpenses} periodLabel={formatPeriod(period)} />
        </div>
      )}
    </div>
  )
}

// 월별 수입/지출 막대그래프 (월별 비교)
function MonthlyChartCard({ totals, description }: { totals: MonthlyTotal[]; description: string }) {
  // 가로축에 보여줄 이름('10월')을 붙인다. 금액은 건드리지 않는다.
  // 1월에는 연도를 붙여서('27년 1월') 기간이 해를 넘기는 것을 알 수 있게 한다.
  const chartData = totals.map((t) => ({
    ...t,
    label: t.month === 1 ? `${t.year % 100}년 1월` : `${t.month}월`,
  }))

  return (
    <Card>
      <CardHeader>
        <CardTitle>월별 수입·지출</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {/* ChartContainer: 부모 폭에 맞춰 차트 크기를 자동으로 바꾼다 (반응형).
            aspect-auto로 기본 비율(16:9)을 끄고 높이를 직접 정한다 */}
        <ChartContainer config={chartConfig} className="aspect-auto h-64 w-full">
          {/* barSize: 막대 두께(px)를 고정한다. 고정하면 같은 달의 두 막대가 붙어서 가운데 정렬된다.
                (모바일 폭에서도 두 막대가 한 달 칸 안에 들어가도록 16으로 정함)
              barGap: 같은 달의 수입·지출 막대 사이 간격(px) */}
          <BarChart data={chartData} barSize={16} barGap={2} accessibilityLayer>
            {/* 가로 눈금선만. 세로선은 막대와 겹쳐 보기 어지럽다 */}
            <CartesianGrid vertical={false} />
            <XAxis dataKey="label" tickLine={false} axisLine={false} />
            {/* tickFormatter: 축 숫자 표시 방법. 3800000 → '380만' */}
            <YAxis tickFormatter={formatMoneyCompact} tickLine={false} axisLine={false} width={48} />
            {/* 막대에 마우스를 올리면 그달 수입·지출 금액을 보여준다 */}
            {/* formatter: 툴팁의 한 줄(수입 또는 지출)을 직접 그린다. 기본 표시는 '원'이 없어서 MoneyText로 바꿨다.
                  formatter를 쓰면 기본 색 점이 빠지므로 색 점도 같이 그린다.
                  name은 <Bar dataKey>의 값('income' | 'expense')이다 */}
            <ChartTooltip
              content={
                <ChartTooltipContent
                  formatter={(value, name) => (
                    <div className="flex w-full items-center gap-2">
                      <span
                        className={cn(
                          'size-2.5 shrink-0 rounded-[2px]',
                          name === 'income' ? 'bg-chart-income' : 'bg-chart-expense',
                        )}
                      />
                      <span className="text-muted-foreground">{name === 'income' ? chartConfig.income.label : chartConfig.expense.label}
                      </span>
                      <MoneyText amount={Number(value)} className="ml-auto font-medium" />
                    </div>
                  )}
                />
              }
            />
            {/* itemSorter={null}: 범례를 이름순(지출→수입)으로 정렬하지 않고 아래 <Bar> 순서(수입→지출) 그대로 둔다 */}
            <ChartLegend content={<ChartLegendContent />} itemSorter={null} />
            {/* radius: 막대 모서리 둥글기 [왼위, 오른위, 오른아래, 왼아래] */}
            <Bar dataKey="income" fill="var(--color-income)" radius={[4, 4, 0, 0]} />
            <Bar dataKey="expense" fill="var(--color-expense)" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ChartContainer>

        {/* 색을 구분하기 어렵거나 정확한 숫자가 필요한 사람을 위해 같은 내용을 표로도 제공한다.
            <details>: 눌러야 펼쳐지는 HTML 기본 요소. 상태(state) 없이 열고 닫을 수 있다 */}
        <details className="text-sm">
          <summary className="cursor-pointer text-muted-foreground">표로 보기</summary>
          <table className="mt-2 w-full tabular-nums">
            <thead className="text-muted-foreground">
              <tr>
                <th className="py-1 text-left font-medium">월</th>
                <th className="py-1 text-right font-medium">수입</th>
                <th className="py-1 text-right font-medium">지출</th>
              </tr>
            </thead>
            <tbody>
              {totals.map((t) => (
                // 여러 해에 걸칠 수 있어서 연·월을 합쳐 key로 쓴다
                <tr key={`${t.year}-${t.month}`} className="border-t">
                  <td className="py-1">
                    {t.year}년 {t.month}월
                  </td>
                  <td className="py-1 text-right">
                    <MoneyText amount={t.income} tone="income" />
                  </td>
                  <td className="py-1 text-right">
                    <MoneyText amount={t.expense} tone="expense" />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </details>
      </CardContent>
    </Card>
  )
}

// 선택한 기간의 카테고리별 지출. 원그래프는 조각 크기 비교가 어려워서 가로 막대 목록으로 보여준다.
function CategoryExpenseCard({ expenses, periodLabel }: { expenses: CategoryExpense[]; periodLabel: string }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>카테고리별 지출</CardTitle>
        <CardDescription>{periodLabel} 지출 비중</CardDescription>
      </CardHeader>
      <CardContent>
        {expenses.length === 0 ? (
          <p className="text-muted-foreground">지출 내역이 없습니다.</p>
        ) : (
          <ul className="flex flex-col gap-4">
            {expenses.map((c) => (
              <li key={c.categoryId} className="flex flex-col gap-1.5">
                <div className="flex items-baseline justify-between gap-4 text-sm">
                  <span className="font-medium">{c.categoryName}</span>
                  {/* whitespace-nowrap: 금액이 아주 길어도 비율이 다음 줄로 내려가지 않게 한다 */}
                  <span className="whitespace-nowrap tabular-nums">
                    <MoneyText amount={c.amount} className="font-semibold" />
                    <span className="ml-2 text-muted-foreground">{formatRatio(c.ratio)}</span>
                  </span>
                </div>
                {/* 비율 막대. 바깥(회색)이 100%, 안쪽 막대 폭이 비율이다.
                    폭은 데이터마다 달라서 Tailwind 클래스로 미리 만들 수 없으므로 style로 지정한다 */}
                <div className="h-2 rounded-full bg-muted">
                  <div className="h-full rounded-full bg-chart-expense" style={{ width: `${c.ratio}%` }} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

export default StatisticsPage
