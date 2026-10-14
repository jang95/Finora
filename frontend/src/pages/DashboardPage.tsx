import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import MoneyText from '@/components/common/MoneyText'
import TransactionRow from '@/components/transaction/TransactionRow'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { dashboardQueryKey, getDashboardSummary } from '@/services/dashboardService'

function DashboardPage() {
  // [useQuery] 데이터 불러오기 (설명은 lib/queryClient.ts, SettingsPage 참고)
  // 이전에는 useState + useEffect + ignore 표시로 직접 불러왔다. 그 일을 useQuery가 대신한다.
  // 거래를 바꾸면 가계부 화면이 ['transactions']를 invalidate → 이름표가 그 아래인 대시보드도 다시 불러온다.
  const { data: summary, error } = useQuery({
    queryKey: dashboardQueryKey,
    queryFn: getDashboardSummary,
  })

  // 불러오기 실패 (서버 꺼짐 등)
  if (error) {
    return (
      <p role="alert" className="text-destructive">
        {error.message}
      </p>
    )
  }

  // 조기 반환: 데이터가 오기 전(undefined)이면 로딩 문구.
  // 이 if 아래에서는 TypeScript가 summary를 undefined가 아닌 DashboardSummary로 인식한다 (타입 좁히기).
  if (summary === undefined) {
    return <p className="text-muted-foreground">불러오는 중...</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">대시보드</h1>

      {/* 모바일 1열 → sm(640px) 2열 → lg(1024px) 4열 */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader>
            {/* 가계부 기준: 지금까지 모든 거래의 수입 − 지출 (결정 D17) */}
            <CardDescription>총자산 (가계부 기준)</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.totalAssets} />
            </CardTitle>
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
            <CardDescription>이번 달 수입</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.monthlyIncome} tone="income" />
            </CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader>
            <CardDescription>이번 달 지출</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.monthlyExpense} tone="expense" />
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
        {summary.recentTransactions.length === 0 ? (
          <p className="px-6 py-4 text-sm text-muted-foreground">거래 내역이 없습니다.</p>
        ) : (
          <ul className="divide-y">
            {summary.recentTransactions.map((t) => (
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
