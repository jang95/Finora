import { WIDGETS } from '@/components/statistics/widgets'
import { cn } from '@/lib/utils'
import type { WidgetLayoutItem } from '@/types/dashboardLayout'
import type { Period } from '@/utils/period'

interface StatisticsDashboardProps {
  layout: WidgetLayoutItem[]
  period: Period
}

// 통계 대시보드: 배치(layout) 순서대로 위젯을 격자에 놓는다. (결정 D26)
// PC(md 이상)는 2열, 모바일은 1열. 드래그·크기 바꾸기·숨기기는 다음 단계에서 붙인다.
function StatisticsDashboard({ layout, period }: StatisticsDashboardProps) {
  return (
    // [CSS Grid] grid-cols-2: 열 2개. gap-4: 칸 사이 간격. md: 접두사 → 화면 폭이 md(768px) 이상일 때만 적용
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
      {layout
        .filter((item) => !item.hidden)
        .map((item) => {
          // 위젯 목록에서 이 id의 컴포넌트를 꺼낸다. 변수 이름이 대문자로 시작해야 JSX가 컴포넌트로 인식한다
          //   (<widget />처럼 소문자면 HTML 태그로 본다)
          const Widget = WIDGETS[item.id].component
          return (
            // col-span-2: 한 줄 전체(2칸)를 차지. 반 칸(half)은 기본값(1칸)
            <div key={item.id} className={cn(item.size === 'full' && 'md:col-span-2')}>
              <Widget period={period} />
            </div>
          )
        })}
    </div>
  )
}

export default StatisticsDashboard
