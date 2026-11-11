import type { ReactNode } from 'react'
import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { EyeOff, GripVertical, Maximize2, Minimize2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { WidgetLayoutItem } from '@/types/dashboardLayout'

interface SortableWidgetProps {
  item: WidgetLayoutItem
  name: string
  isEditing: boolean
  onToggleSize: () => void
  onHide: () => void
  children: ReactNode
}

// 격자 한 칸: 위젯을 감싸고, 편집 모드에서는 위에 도구 줄(손잡이·크기·숨기기)을 붙인다.
// [useSortable] dnd-kit 훅. 이 요소를 "정렬 가능한 항목"으로 등록하고 드래그에 필요한 것들을 돌려준다:
//   setNodeRef  - 어느 DOM 요소가 이 항목인지 dnd-kit에 알려준다 (ref)
//   attributes  - 접근성 속성 (role, aria-*, tabIndex). 키보드로도 옮길 수 있게 한다
//   listeners   - 드래그를 시작하는 이벤트 처리기 (onPointerDown, onKeyDown). 손잡이에만 붙여서 손잡이로만 끌게 한다
//   transform   - 드래그 중 이 항목이 밀려나야 할 거리 (x, y). 실제로 DOM 순서를 바꾸지 않고 이동만 보여준다
//   transition  - 밀려날 때의 애니메이션
//   isDragging  - 지금 끌고 있는 항목인지
function SortableWidget({ item, name, isEditing, onToggleSize, onHide, children }: SortableWidgetProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: item.id,
    disabled: !isEditing, // 편집 모드가 아니면 드래그 자체를 끈다
  })

  return (
    <div
      ref={setNodeRef}
      // transform은 { x, y, scaleX, scaleY } 객체라서 CSS 문자열('translate3d(...)')로 바꿔 준다.
      // 값은 드래그마다 달라서 Tailwind 클래스로 못 만들고 style로 넣는다 (CategoryExpenseBar의 width와 같은 이유)
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn(
        'flex flex-col',
        item.size === 'full' && 'md:col-span-2',
        // 끌고 있는 항목은 다른 카드 위로 보이게(z-10) 하고 살짝 투명하게
        isDragging && 'z-10 opacity-80',
      )}
    >
      {isEditing && (
        <div className="flex items-center gap-1 rounded-t-xl border border-b-0 border-dashed bg-muted/50 px-2 py-1 text-sm">
          {/* 드래그 손잡이. touch-none: 폰에서 손잡이를 누른 채 움직일 때 화면이 스크롤되지 않게 한다 */}
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="cursor-grab touch-none active:cursor-grabbing"
            aria-label={`${name} 옮기기`}
            {...attributes}
            {...listeners}
          >
            <GripVertical />
          </Button>
          <span className="flex-1 truncate font-medium">{name}</span>
          {/* 크기 바꾸기: 반 칸 ↔ 한 줄. 모바일(1열)에서는 차이가 없어서 md 이상에서만 보여준다 */}
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="hidden md:inline-flex"
            aria-label={item.size === 'half' ? '한 줄로 넓히기' : '반 칸으로 줄이기'}
            title={item.size === 'half' ? '한 줄로 넓히기' : '반 칸으로 줄이기'}
            onClick={onToggleSize}
          >
            {item.size === 'half' ? <Maximize2 /> : <Minimize2 />}
          </Button>
          <Button type="button" variant="ghost" size="icon-sm" aria-label={`${name} 숨기기`} title="숨기기" onClick={onHide}>
            <EyeOff />
          </Button>
        </div>
      )}
      {/* 편집 모드에서는 위젯 안쪽 클릭(카테고리 펼치기 등)을 막고 흐리게: 지금은 배치를 바꾸는 중이라는 표시.
          pointer-events-none: 마우스·터치 이벤트를 받지 않게 한다 */}
      <div className={cn('min-h-0 flex-1', isEditing && 'pointer-events-none select-none opacity-70')}>{children}</div>
    </div>
  )
}

export default SortableWidget
