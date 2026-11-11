import { useState } from 'react'
import { Check, RotateCcw, SlidersHorizontal } from 'lucide-react'
import PeriodPicker from '@/components/common/PeriodPicker'
import StatisticsDashboard from '@/components/statistics/StatisticsDashboard'
import { DEFAULT_LAYOUT } from '@/components/statistics/widgets'
import { Button } from '@/components/ui/button'
import { getPeriod } from '@/utils/period'
import { getToday } from '@/utils/yearMonth'

// 통계 화면 = 기간 선택 + 위젯 대시보드 (결정 D26).
// 화면은 기간과 배치만 들고 있고, 데이터는 위젯이 각자 불러온다 (이유는 PeriodSummaryWidget 참고).
// 배치는 아직 메모리에만 있어서 새로고침하면 기본 배치로 돌아간다. 사용자별 저장은 다음 단계 (결정 D27).
function StatisticsPage() {
  // 고른 기간. 처음엔 이번 달. 대시보드 화면과 공유하지 않는다 (화면마다 따로)
  const [period, setPeriod] = useState(() => getPeriod('month', getToday()))
  // 위젯 배치 (순서·크기·숨김). 바꾸는 함수는 utils/dashboardLayout.ts
  const [layout, setLayout] = useState(DEFAULT_LAYOUT)
  // 편집 모드: 켜면 위젯 위에 손잡이·크기·숨기기 도구가 나오고 드래그할 수 있다 (결정 D26)
  const [isEditing, setIsEditing] = useState(false)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">통계</h1>
        <div className="flex items-center gap-2">
          {isEditing && (
            <Button type="button" variant="ghost" size="sm" onClick={() => setLayout(DEFAULT_LAYOUT)}>
              <RotateCcw />
              기본 배치
            </Button>
          )}
          {/* 같은 버튼이 "배치 편집" ↔ "완료"로 바뀐다 */}
          <Button type="button" variant={isEditing ? 'default' : 'outline'} size="sm" onClick={() => setIsEditing((v) => !v)}>
            {isEditing ? <Check /> : <SlidersHorizontal />}
            {isEditing ? '완료' : '배치 편집'}
          </Button>
        </div>
      </div>

      <PeriodPicker value={period} onChange={setPeriod} />

      <StatisticsDashboard layout={layout} period={period} isEditing={isEditing} onLayoutChange={setLayout} />
    </div>
  )
}

export default StatisticsPage
