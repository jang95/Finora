import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { addMonths, type YearMonth } from '@/utils/yearMonth'

interface MonthNavigatorProps {
  value: YearMonth
  onChange: (value: YearMonth) => void
  // 'year'면 "< 2026년 >"로 보여주고 한 해씩 이동한다 (월은 그대로). 기본은 'month'
  unit?: 'month' | 'year'
}

// "< 2026년 10월 >" 월 이동 버튼. 가계부·통계 화면에서 같이 쓴다.
// 선택된 월은 부모가 state로 가지고, 이 컴포넌트는 보여주고 바꿔달라고 알리기만 한다 (TransactionFormDialog의 open과 같은 방식)
function MonthNavigator({ value, onChange, unit = 'month' }: MonthNavigatorProps) {
  const isYear = unit === 'year'
  const step = isYear ? 12 : 1
  const unitLabel = isYear ? '해' : '달'

  return (
    <div className="flex items-center gap-1">
      {/* 아이콘만 있는 버튼은 화면낭독기가 읽을 이름이 없으므로 aria-label을 붙인다 */}
      <Button variant="ghost" size="icon" aria-label={`이전 ${unitLabel}`} onClick={() => onChange(addMonths(value, -step))}>
        <ChevronLeft />
      </Button>
      <span className="min-w-28 text-center font-medium tabular-nums">
        {value.year}년{!isYear && ` ${value.month}월`}
      </span>
      <Button variant="ghost" size="icon" aria-label={`다음 ${unitLabel}`} onClick={() => onChange(addMonths(value, step))}>
        <ChevronRight />
      </Button>
    </div>
  )
}

export default MonthNavigator
