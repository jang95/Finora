import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import { SortableContext, rectSortingStrategy, sortableKeyboardCoordinates } from '@dnd-kit/sortable'
import { Eye } from 'lucide-react'
import SortableWidget from '@/components/statistics/SortableWidget'
import { WIDGETS } from '@/components/statistics/widgets'
import { Button } from '@/components/ui/button'
import type { WidgetId, WidgetLayoutItem } from '@/types/dashboardLayout'
import { moveWidget, setWidgetHidden, setWidgetSize } from '@/utils/dashboardLayout'
import type { Period } from '@/utils/period'

interface StatisticsDashboardProps {
  layout: WidgetLayoutItem[]
  period: Period
  isEditing: boolean
  // 배치가 바뀌면 새 배치를 부모(StatisticsPage)에게 알린다. state는 부모가 가진다 (아래 설명)
  onLayoutChange: (layout: WidgetLayoutItem[]) => void
}

// 통계 대시보드: 배치(layout) 순서대로 위젯을 격자에 놓는다. (결정 D26)
// PC(md 이상)는 2열, 모바일은 1열. 편집 모드에서는 드래그로 순서, 크기, 숨기기를 바꾼다.
// [state 끌어올리기] 배치 state는 여기가 아니라 StatisticsPage가 가진다. 편집 버튼(페이지)과 격자(여기)가
//   같은 배치를 봐야 하고, 다음 단계에서 Backend에 저장할 때도 페이지에서 하기 때문. 여기는 "바뀌었다"고 알리기만 한다.
function StatisticsDashboard({ layout, period, isEditing, onLayoutChange }: StatisticsDashboardProps) {
  // [센서] dnd-kit이 "드래그 시작"을 어떤 입력으로 감지할지.
  //   PointerSensor: 마우스·터치·펜 모두. distance: 5 → 5px 이상 움직여야 드래그로 본다 (그냥 누른 건 클릭)
  //   KeyboardSensor: 손잡이에 초점을 두고 Space → 화살표 → Space 로 옮긴다 (마우스를 못 쓰는 사람용)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  const visible = layout.filter((item) => !item.hidden)
  const hidden = layout.filter((item) => item.hidden)

  // 드래그가 끝났을 때. active = 끌던 항목, over = 놓은 자리의 항목 (격자 밖에 놓으면 null)
  function handleDragEnd({ active, over }: DragEndEvent) {
    if (!over || active.id === over.id) return
    // dnd-kit의 id 타입은 string | number 라서 우리 WidgetId로 알려준다 (as: 타입 단언)
    onLayoutChange(moveWidget(layout, active.id as WidgetId, over.id as WidgetId))
  }

  return (
    <div className="flex flex-col gap-4">
      {/* [DndContext] 드래그 앤 드롭이 일어나는 범위. 센서와 "어느 항목 위에 있는지" 판단 방법(closestCenter: 중심이 가장 가까운 항목)을 정한다.
          [SortableContext] 그 안에서 "정렬 가능한 항목 목록". items에는 id 배열을 화면 순서대로 준다.
            rectSortingStrategy: 격자(여러 열)에서 항목이 밀려나는 방식. 세로 목록이면 verticalListSortingStrategy를 쓴다 */}
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <SortableContext items={visible.map((item) => item.id)} strategy={rectSortingStrategy}>
          {/* [CSS Grid] grid-cols-2: 열 2개. gap-4: 칸 사이 간격. md: 접두사 → 화면 폭이 md(768px) 이상일 때만 적용 */}
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            {visible.map((item) => {
              // 위젯 목록에서 이 id의 컴포넌트를 꺼낸다. 변수 이름이 대문자로 시작해야 JSX가 컴포넌트로 인식한다
              //   (<widget />처럼 소문자면 HTML 태그로 본다)
              const Widget = WIDGETS[item.id].component
              return (
                <SortableWidget
                  key={item.id}
                  item={item}
                  name={WIDGETS[item.id].name}
                  isEditing={isEditing}
                  onToggleSize={() => onLayoutChange(setWidgetSize(layout, item.id, item.size === 'half' ? 'full' : 'half'))}
                  onHide={() => onLayoutChange(setWidgetHidden(layout, item.id, true))}
                >
                  <Widget period={period} />
                </SortableWidget>
              )
            })}
          </div>
        </SortableContext>
      </DndContext>

      {/* 숨긴 위젯 목록: 편집 모드에서만. 누르면 다시 보인다 */}
      {isEditing && hidden.length > 0 && (
        <div className="flex flex-wrap items-center gap-2 rounded-xl border border-dashed p-3 text-sm">
          <span className="text-muted-foreground">숨긴 위젯:</span>
          {hidden.map((item) => (
            <Button key={item.id} type="button" variant="outline" size="sm" onClick={() => onLayoutChange(setWidgetHidden(layout, item.id, false))}>
              <Eye />
              {WIDGETS[item.id].name}
            </Button>
          ))}
        </div>
      )}
    </div>
  )
}

export default StatisticsDashboard
