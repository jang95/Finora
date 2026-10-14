import { useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import MoneyText from '@/components/common/MoneyText'
import PeriodPicker from '@/components/common/PeriodPicker'
import TransactionRow from '@/components/transaction/TransactionRow'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import { dashboardQueryKey, getDashboardSummary } from '@/services/dashboardService'
import { getPeriodSummary, periodSummaryQueryKey } from '@/services/statisticsService'
import { formatDate } from '@/utils/format'
import { getPeriod } from '@/utils/period'
import { getToday } from '@/utils/yearMonth'

function DashboardPage() {
  // 고른 기간. 처음엔 이번 달. 화면마다 따로 가진다 (통계 화면과 공유하지 않음)
  // useState(() => ...): 처음 한 번만 계산한다 (다시 그릴 때마다 getPeriod를 부르지 않게)
  const [period, setPeriod] = useState(() => getPeriod('month', getToday()))

  // [useQuery] 데이터 불러오기 (설명은 lib/queryClient.ts, SettingsPage 참고)
  // 거래를 바꾸면 가계부 화면이 ['transactions']를 invalidate → 이름표가 그 아래인 두 데이터도 다시 불러온다.
  // 최근 거래: 기간과 상관없이 최신 5건
  const dashboardQuery = useQuery({
    queryKey: dashboardQueryKey,
    queryFn: getDashboardSummary,
  })
  // 기간 요약: 이름표에 from, to가 있어서 기간을 바꾸면 새로 불러온다.
  // placeholderData: keepPreviousData → 새 기간을 불러오는 동안 이전 기간 숫자를 그대로 보여준다
  //   (없으면 기간을 바꿀 때마다 "불러오는 중..."으로 화면이 깜빡인다)
  const summaryQuery = useQuery({
    queryKey: periodSummaryQueryKey(period.from, period.to),
    queryFn: () => getPeriodSummary(period.from, period.to),
    placeholderData: keepPreviousData,
  })

  const dashboard = dashboardQuery.data
  const summary = summaryQuery.data

  // 불러오기 실패 (서버 꺼짐 등). ?? : 앞의 값이 null/undefined면 뒤의 값
  const loadError = dashboardQuery.error ?? summaryQuery.error
  if (loadError) {
    return (
      <p role="alert" className="text-destructive">
        {loadError.message}
      </p>
    )
  }

  // 조기 반환: 데이터가 오기 전(undefined)이면 로딩 문구.
  // 이 if 아래에서는 TypeScript가 둘 다 undefined가 아니라고 인식한다 (타입 좁히기).
  if (dashboard === undefined || summary === undefined) {
    return <p className="text-muted-foreground">불러오는 중...</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">대시보드</h1>

      <PeriodPicker value={period} onChange={setPeriod} />

      {/* 모바일 1열 → sm(640px) 2열 → lg(1024px) 4열 */}
      {/* isPlaceholderData: 지금 보이는 숫자가 이전 기간 것(새 기간 불러오는 중)이면 흐리게 */}
      <div className={cn('grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4', summaryQuery.isPlaceholderData && 'opacity-60')}>
        <Card>
          <CardHeader>
            {/* 가계부 기준: 기간 마지막 날까지 모든 거래의 수입 − 지출 (결정 D17) */}
            <CardDescription>총자산 (가계부 기준)</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.totalAssets} />
            </CardTitle>
            <CardDescription className="text-xs">{formatDate(summary.to)} 기준</CardDescription>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader>
            {/* 계좌 기준: 계좌 잔액 합계. 계좌 기능(Phase 8)이 생기면 값을 연결한다 (결정 D16, D17) */}
            <CardDescription>총자산 (계좌 기준)</CardDescription>
            <CardTitle className="text-2xl font-bold text-muted-foreground">준비 중</CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader>
            <CardDescription>수입</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.income} tone="income" />
            </CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader>
            <CardDescription>지출</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.expense} tone="expense" />
            </CardTitle>
          </CardHeader>
        </Card>
      </div>

      <Card className="gap-0 pb-0">
        {/* CardAction: 카드 제목 줄의 오른쪽 끝 자리 (shadcn Card 구성 요소) */}
        <CardHeader className="border-b pb-4">
          <CardTitle>최근 거래</CardTitle>
          <CardAction>
            <Button variant="link" size="sm" className="h-auto p-0" asChild>
              <Link to="/transactions">전체 보기</Link>
            </Button>
          </CardAction>
        </CardHeader>
        {dashboard.recentTransactions.length === 0 ? (
          <p className="px-6 py-4 text-sm text-muted-foreground">거래 내역이 없습니다.</p>
        ) : (
          <ul className="divide-y">
            {dashboard.recentTransactions.map((t) => (
              <li key={t.id}>
                <TransactionRow transaction={t} showDate />
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  )
}

export default DashboardPage
