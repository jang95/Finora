import { arrayMove } from '@dnd-kit/sortable'
import type { WidgetId, WidgetLayoutItem, WidgetSize } from '@/types/dashboardLayout'

// 위젯 배치를 바꾸는 순수 함수 모음 (결정 D26).
// [순수 함수] 받은 배열을 고치지 않고 "바뀐 새 배열"을 돌려준다. React state는 새 값으로 바꿔야 화면이 다시 그려지기 때문.
//   화면 코드와 떼어 놓으면 나중에 자동 테스트로 확인하기도 쉽다.

// activeId 위젯을 overId 위젯 자리로 옮긴다 (드래그가 끝났을 때).
// 숨긴 위젯은 화면에 없지만 배열에는 그대로 있어서, 보이는 위젯만 자리를 바꾸고 숨긴 것의 순서는 유지된다.
export function moveWidget(layout: WidgetLayoutItem[], activeId: WidgetId, overId: WidgetId): WidgetLayoutItem[] {
  const from = layout.findIndex((item) => item.id === activeId)
  const to = layout.findIndex((item) => item.id === overId)
  if (from < 0 || to < 0 || from === to) return layout
  // arrayMove(배열, from, to): dnd-kit이 주는 도우미. from 위치의 항목을 빼서 to 위치에 끼운 새 배열을 돌려준다
  return arrayMove(layout, from, to)
}

// 한 위젯의 크기를 바꾼다. map으로 전체를 돌면서 해당 id만 새 객체({ ...item, size })로 바꾼다
export function setWidgetSize(layout: WidgetLayoutItem[], id: WidgetId, size: WidgetSize): WidgetLayoutItem[] {
  return layout.map((item) => (item.id === id ? { ...item, size } : item))
}

// 한 위젯을 숨기거나(hidden: true) 다시 보이게(hidden: false) 한다. 배열에서 빼지 않아서 순서가 남는다
export function setWidgetHidden(layout: WidgetLayoutItem[], id: WidgetId, hidden: boolean): WidgetLayoutItem[] {
  return layout.map((item) => (item.id === id ? { ...item, hidden } : item))
}
