import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatPeriod, getPeriod, shiftPeriod, type Period, type PeriodUnit } from '@/utils/period'
import { getToday } from '@/utils/yearMonth'

interface PeriodPickerProps {
  value: Period
  onChange: (value: Period) => void
}

const UNIT_OPTIONS: { value: PeriodUnit; label: string }[] = [
  { value: 'day', label: '일간' },
  { value: 'week', label: '주간' },
  { value: 'month', label: '월간' },
  { value: 'year', label: '연간' },
]

// 기간 선택: [일간|주간|월간|연간] 탭 + "< 2026년 10월 >" 이동. 대시보드·통계 화면에서 같이 쓴다. (결정 D17)
// MonthNavigator처럼 선택된 기간은 부모가 state로 가지고, 이 컴포넌트는 보여주고 바꿔달라고 알리기만 한다.
function PeriodPicker({ value, onChange }: PeriodPickerProps) {
  // 단위를 바꿀 때 기준 날짜: 지금 보는 기간에 오늘이 들어 있으면 오늘, 아니면 그 기간의 첫날.
  // 예) 이번 달을 보다가 "일간" → 오늘 / 지난달을 보다가 "일간" → 지난달 1일
  // 'YYYY-MM-DD' 문자열은 사전순 = 날짜순이라 문자열 비교(<=)로 범위를 확인할 수 있다.
  function changeUnit(unit: PeriodUnit) {
    const today = getToday()
    const base = value.from <= today && today <= value.to ? today : value.from
    onChange(getPeriod(unit, base))
  }

  return (
    <div className="flex flex-col items-start gap-2 sm:flex-row sm:items-center sm:gap-4">
      {/* onValueChange는 문자열을 넘겨주므로 PeriodUnit으로 알려준다 (탭 값이 UNIT_OPTIONS뿐이라 안전) */}
      <Tabs value={value.unit} onValueChange={(unit) => changeUnit(unit as PeriodUnit)}>
        <TabsList>
          {UNIT_OPTIONS.map((option) => (
            <TabsTrigger key={option.value} value={option.value}>
              {option.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <div className="flex items-center gap-1">
        <Button variant="ghost" size="icon" aria-label="이전 기간" onClick={() => onChange(shiftPeriod(value, -1))}>
          <ChevronLeft />
        </Button>
        <span className="min-w-28 text-center font-medium tabular-nums">{formatPeriod(value)}</span>
        <Button variant="ghost" size="icon" aria-label="다음 기간" onClick={() => onChange(shiftPeriod(value, 1))}>
          <ChevronRight />
        </Button>
      </div>
    </div>
  )
}

export default PeriodPicker
