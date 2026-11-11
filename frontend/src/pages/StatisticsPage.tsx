import { useState } from 'react'
import PeriodPicker from '@/components/common/PeriodPicker'
import StatisticsDashboard from '@/components/statistics/StatisticsDashboard'
import { DEFAULT_LAYOUT } from '@/components/statistics/widgets'
import { getPeriod } from '@/utils/period'
import { getToday } from '@/utils/yearMonth'

// 통계 화면 = 기간 선택 + 위젯 대시보드 (결정 D26).
// 화면은 기간만 들고 있고, 데이터는 위젯이 각자 불러온다 (이유는 PeriodSummaryWidget 참고).
// 배치는 지금은 기본 배치 고정. 드래그로 바꾸고 사용자별로 저장하는 것은 다음 단계 (결정 D27).
function StatisticsPage() {
  // 고른 기간. 처음엔 이번 달. 대시보드 화면과 공유하지 않는다 (화면마다 따로)
  const [period, setPeriod] = useState(() => getPeriod('month', getToday()))

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">통계</h1>

      <PeriodPicker value={period} onChange={setPeriod} />

      <StatisticsDashboard layout={DEFAULT_LAYOUT} period={period} />
    </div>
  )
}

export default StatisticsPage
