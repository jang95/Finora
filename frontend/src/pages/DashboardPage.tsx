import { useEffect, useState } from 'react'
import MoneyText from '@/components/common/MoneyText'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { getDashboardSummary } from '@/services/dashboardService'
import type { DashboardSummary } from '@/types/dashboard'
import { formatPercent } from '@/utils/format'

function DashboardPage() {
  // [useState] 화면이 기억하는 값(state).
  // - 반환값은 [현재 값, 값을 바꾸는 함수] 두 개짜리 배열이고, 구조 분해로 이름을 붙인다.
  // - 컴포넌트 함수는 화면을 그릴 때마다 처음부터 다시 실행된다. 그래서 일반 변수(let)는 매번 초기화되고,
  //   값을 바꿔도 React가 모르기 때문에 화면이 갱신되지 않는다.
  //   useState 값은 React가 따로 보관하므로 다시 실행돼도 유지되고, setSummary(새 값)를 부르면 화면을 다시 그린다.
  // - 값은 직접 바꾸지 않는다 (summary.totalAssets = 0 ✗). 항상 setSummary로 새 값을 넘긴다.
  // - <DashboardSummary | null>: "DashboardSummary 또는 null"이라는 타입. 데이터가 오기 전이라 null로 시작한다.
  const [summary, setSummary] = useState<DashboardSummary | null>(null)

  // [useEffect] 화면이 그려진 "뒤에" 실행할 작업 (데이터 불러오기, 타이머 등 화면 밖과 주고받는 일).
  // - 컴포넌트 본문에서 바로 service를 부르면 다시 그릴 때마다 호출되고, set → 다시 그림 → 또 호출... 무한 반복된다.
  // - 두 번째 인자(의존성 배열)는 "언제 다시 실행할지"를 정한다.
  //     []        → 화면이 처음 나타날 때 한 번만
  //     [month]   → 처음 + month 값이 바뀔 때마다 (가계부 월 이동에서 쓸 예정)
  //     생략       → 그릴 때마다 (거의 쓰지 않음)
  // - 개발 모드(StrictMode)에서는 문제를 일찍 찾도록 일부러 "실행 → 정리 → 다시 실행"을 한 번 더 한다.
  //   그래서 개발 중에는 호출이 두 번 보일 수 있다. 배포 빌드에서는 한 번만 실행된다.
  useEffect(() => {
    // 응답이 오기 전에 다른 메뉴로 이동하면, 이미 사라진 화면의 state를 바꾸지 않도록 막는 표시.
    // 실제 API는 응답 시간이 제각각이라, 늦게 온 응답이 최신 값을 덮어쓰는 문제도 함께 막는다.
    let ignore = false

    // .then(): Promise의 값이 도착하면 실행할 함수를 등록한다
    getDashboardSummary().then((data) => {
      if (!ignore) setSummary(data)
    })

    // 정리(cleanup) 함수: 화면이 사라질 때, 또는 effect를 다시 실행하기 직전에 React가 호출한다
    return () => {
      ignore = true
    }
  }, [])

  // 조기 반환: 데이터가 없으면 아래 화면 대신 로딩 문구를 그린다.
  // 이 if 아래에서는 TypeScript가 summary를 null이 아닌 DashboardSummary로 인식한다 (타입 좁히기).
  if (summary === null) {
    return <p className="text-muted-foreground">불러오는 중...</p>
  }

  const { investment } = summary

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">대시보드</h1>

      {/* 모바일 1열 → sm(640px) 2열 → xl(1280px) 4열 */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader>
            <CardDescription>총자산</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={summary.totalAssets} />
            </CardTitle>
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

        <Card>
          <CardHeader>
            <CardDescription>투자 평가금액</CardDescription>
            <CardTitle className="text-2xl font-bold">
              <MoneyText amount={investment.totalValue} />
            </CardTitle>
          </CardHeader>
          <CardContent className="flex gap-2 text-sm">
            <MoneyText amount={investment.profit} tone="profit" />
            <span className="text-muted-foreground">({formatPercent(investment.profitRate)})</span>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default DashboardPage
