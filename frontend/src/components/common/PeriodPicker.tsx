import { useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { DateRange } from 'react-day-picker'
import { ko } from 'react-day-picker/locale'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverAnchor, PopoverContent } from '@/components/ui/popover'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { customPeriod, dateToKey, formatPeriod, getPeriod, parseDate, shiftPeriod, type Period, type PeriodUnit } from '@/utils/period'
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
  { value: 'custom', label: '직접' },
]

// 기간 선택: [일간|주간|월간|연간|직접] 탭 + "< 2026년 10월 >" 이동. 대시보드·통계 화면에서 같이 쓴다. (결정 D17)
// MonthNavigator처럼 선택된 기간은 부모가 state로 가지고, 이 컴포넌트는 보여주고 바꿔달라고 알리기만 한다.
// "직접"을 누르면 달력 창(Popover)이 열리고, 시작·끝 날짜를 고른 뒤 "적용"을 눌러야 기간이 바뀐다.
function PeriodPicker({ value, onChange }: PeriodPickerProps) {
  // 달력 창이 열려 있는지
  const [calendarOpen, setCalendarOpen] = useState(false)
  // 달력에서 고르는 중인 범위. "적용" 전까지는 부모의 기간을 바꾸지 않는다.
  // DateRange: react-day-picker의 타입 { from: Date | undefined, to?: Date }
  const [draft, setDraft] = useState<DateRange | undefined>()

  function openCalendar() {
    // 지금 보는 기간을 달력의 처음 선택으로 보여준다
    setDraft({ from: parseDate(value.from), to: parseDate(value.to) })
    setCalendarOpen(true)
  }

  // 단위를 바꿀 때 기준 날짜: 지금 보는 기간에 오늘이 들어 있으면 오늘, 아니면 그 기간의 첫날.
  // 예) 이번 달을 보다가 "일간" → 오늘 / 지난달을 보다가 "일간" → 지난달 1일
  // 'YYYY-MM-DD' 문자열은 사전순 = 날짜순이라 문자열 비교(<=)로 범위를 확인할 수 있다.
  function changeUnit(unit: PeriodUnit) {
    // "직접"은 바로 바꾸지 않고 달력을 연다 (탭은 "적용"한 뒤에 "직접"으로 바뀐다)
    if (unit === 'custom') {
      openCalendar()
      return
    }
    const today = getToday()
    const base = value.from <= today && today <= value.to ? today : value.from
    onChange(getPeriod(unit, base))
  }

  function applyDraft() {
    // ?.: draft가 없으면(undefined) 에러 대신 undefined를 돌려준다 (옵셔널 체이닝)
    if (!draft?.from) return
    // 끝 날짜를 안 골랐으면 시작 날짜 하루만
    const from = dateToKey(draft.from)
    const to = draft.to ? dateToKey(draft.to) : from
    onChange(customPeriod(from, to))
    setCalendarOpen(false)
  }

  return (
    // Popover: 버튼 근처에 뜨는 작은 창. open/onOpenChange로 열림 상태를 이 컴포넌트가 관리한다 (바깥 클릭·Esc면 닫힘)
    <Popover open={calendarOpen} onOpenChange={setCalendarOpen}>
      <div className="flex flex-col items-start gap-2 sm:flex-row sm:items-center sm:gap-4">
        {/* PopoverAnchor: 창을 이 탭 줄 아래에 띄운다 (따로 여는 버튼이 없어서 위치 기준만 정함) */}
        <PopoverAnchor asChild>
          {/* onValueChange는 문자열을 넘겨주므로 PeriodUnit으로 알려준다 (탭 값이 UNIT_OPTIONS뿐이라 안전) */}
          <Tabs value={value.unit} onValueChange={(unit) => changeUnit(unit as PeriodUnit)}>
            <TabsList>
              {UNIT_OPTIONS.map((option) => (
                // 이미 "직접"이 선택된 상태에서 다시 누르면 onValueChange가 불리지 않으므로 onClick으로도 연다
                <TabsTrigger
                  key={option.value}
                  value={option.value}
                  onClick={option.value === 'custom' && value.unit === 'custom' ? openCalendar : undefined}
                >
                  {option.label}
                </TabsTrigger>
              ))}
            </TabsList>
          </Tabs>
        </PopoverAnchor>

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

      <PopoverContent className="w-auto p-0" align="start">
        {/* mode="range": 시작·끝 두 날짜를 고르는 달력. locale={ko}: 요일·월 이름을 한국어로 */}
        <Calendar mode="range" selected={draft} onSelect={setDraft} defaultMonth={draft?.from} locale={ko} />
        <div className="flex justify-end gap-2 border-t p-3">
          <Button variant="ghost" size="sm" onClick={() => setCalendarOpen(false)}>
            취소
          </Button>
          <Button size="sm" onClick={applyDraft} disabled={!draft?.from}>
            적용
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  )
}

export default PeriodPicker
