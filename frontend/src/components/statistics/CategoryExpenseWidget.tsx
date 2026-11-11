import { Fragment, useState } from 'react'
import { ChevronRight } from 'lucide-react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import MoneyText from '@/components/common/MoneyText'
import WidgetCard from '@/components/statistics/WidgetCard'
import type { WidgetProps } from '@/components/statistics/widgets'
import { Button } from '@/components/ui/button'
import { categoryExpensesQueryKey, getCategoryExpenses } from '@/services/statisticsService'
import type { CategoryExpense } from '@/types/statistics'
import { formatRatio } from '@/utils/format'
import { formatPeriod } from '@/utils/period'

// 카테고리별 지출에서 펼쳐 들어간 한 단계
interface DrillStep {
  id: number
  name: string
}

// 위젯 ③ 선택한 기간의 카테고리별 지출. 원그래프는 조각 크기 비교가 어려워서 가로 막대 목록으로 보여준다.
// 처음엔 대분류별 합계(하위 포함). 하위 분류가 있는 줄을 누르면 그 아래 단계가 보이고, 위쪽 경로를 눌러 되돌아간다.
function CategoryExpenseWidget({ period }: WidgetProps) {
  // 펼쳐 들어간 경로. 비어 있으면 대분류 단계. 예: [교통, 대중교통] (결정 D23)
  // 기간을 바꿔도 펼친 위치는 유지한다 (같은 카테고리를 다른 달과 비교하기 쉽게).
  // 이 state는 이 위젯만 쓰므로 화면(page)이 아니라 위젯 안에 둔다.
  const [drillPath, setDrillPath] = useState<DrillStep[]>([])
  // [at(-1)] 배열의 마지막 항목. 비어 있으면 undefined → ?? null로 "대분류 단계"
  const parentId = drillPath.at(-1)?.id ?? null

  // [useQuery] 설명은 PeriodSummaryWidget 참고. parentId도 이름표에 있어서 단계마다 따로 캐시된다.
  const categoryQuery = useQuery({
    queryKey: categoryExpensesQueryKey(period.from, period.to, parentId),
    queryFn: () => getCategoryExpenses(period.from, period.to, parentId),
    placeholderData: keepPreviousData,
  })
  const expenses = categoryQuery.data

  return (
    <WidgetCard
      title="카테고리별 지출"
      description={`${formatPeriod(period)} 지출 비중`}
      isStale={categoryQuery.isPlaceholderData}
      error={categoryQuery.error}
      isLoading={expenses === undefined}
    >
      {/* 펼친 경로: 전체 › 교통 › 대중교통. 앞 단계를 누르면 그 단계로 돌아간다. 대분류 단계에서는 숨긴다 */}
      {drillPath.length > 0 && (
        <nav aria-label="카테고리 경로" className="flex flex-wrap items-center gap-1 text-sm">
          <Button variant="link" size="sm" className="h-auto px-0" onClick={() => setDrillPath([])}>
            전체
          </Button>
          {drillPath.map((step, index) => (
            <Fragment key={step.id}>
              <ChevronRight className="size-4 text-muted-foreground" aria-hidden />
              {index === drillPath.length - 1 ? (
                // 지금 보고 있는 단계는 누를 필요가 없어서 글자로만. aria-current: 화면낭독기에 "현재 위치"라고 알린다
                <span aria-current="page" className="font-medium">
                  {step.name}
                </span>
              ) : (
                // slice(0, index + 1): 처음부터 이 단계까지만 남긴다 = 이 단계로 돌아가기
                <Button variant="link" size="sm" className="h-auto px-0" onClick={() => setDrillPath(drillPath.slice(0, index + 1))}>
                  {step.name}
                </Button>
              )}
            </Fragment>
          ))}
        </nav>
      )}

      {expenses && expenses.length === 0 ? (
        <p className="text-muted-foreground">지출 내역이 없습니다.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {expenses?.map((c) => (
            // 같은 카테고리 Id가 "대중교통"(하위 묶음)과 "대중교통 (세부 없음)"(직접 기록)으로 두 번 나올 수 있어서 key에 구분을 넣는다
            <li key={`${c.categoryId}-${c.isDirect}`}>
              {c.hasChildren ? (
                // 펼칠 수 있는 줄은 버튼. [...drillPath, 새 단계]: 기존 배열을 고치지 않고 끝에 하나 붙인 새 배열을 만든다
                //   (React state는 직접 고치지 않고 새 값으로 바꿔야 화면이 다시 그려진다)
                <button
                  type="button"
                  className="w-full rounded-md p-2 text-left hover:bg-muted"
                  onClick={() => setDrillPath([...drillPath, { id: c.categoryId, name: c.categoryName }])}
                >
                  <CategoryExpenseBar expense={c} />
                </button>
              ) : (
                <div className="p-2">
                  <CategoryExpenseBar expense={c} />
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </WidgetCard>
  )
}

// 카테고리 한 줄: 이름 · 금액 · 비율 + 비율 막대
function CategoryExpenseBar({ expense: c }: { expense: CategoryExpense }) {
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between gap-4 text-sm">
        <span className="flex items-center gap-1 font-medium">
          {c.categoryName}
          {c.isDirect && <span className="font-normal text-muted-foreground">(세부 없음)</span>}
          {/* 펼칠 수 있다는 표시 */}
          {c.hasChildren && <ChevronRight className="size-4 text-muted-foreground" aria-hidden />}
        </span>
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
    </div>
  )
}

export default CategoryExpenseWidget
