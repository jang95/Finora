import { arrayMove } from '@dnd-kit/sortable'
import type { StoredLayoutItem, WidgetId, WidgetLayoutItem, WidgetSize } from '@/types/dashboardLayout'

// 서버에 저장된 배치를 화면에서 쓸 배치로 바꾼다 (결정 D27):
//   1) 모르는 위젯 id는 버린다 (옛날 배치에 남은, 지금은 없는 위젯)
//   2) 저장된 배치에 없는 위젯은 기본 배치 순서로 뒤에 붙인다 (새로 만든 위젯이 저절로 나타남)
// 저장한 적이 없으면(stored = []) 결과는 기본 배치 그대로다.
export function mergeLayout(stored: StoredLayoutItem[], defaultLayout: WidgetLayoutItem[]): WidgetLayoutItem[] {
  // 기본 배치에 있는 id = 아는 위젯. Set: 값의 모음. has()가 배열 includes()보다 빠르고 뜻도 분명하다
  const knownIds = new Set<string>(defaultLayout.map((item) => item.id))
  const seen = new Set<string>()
  const merged: WidgetLayoutItem[] = []
  for (const item of stored) {
    // 모르는 id이거나 같은 id가 두 번 나오면 건너뛴다. Backend도 검사하지만 화면도 스스로 지킨다
    if (!knownIds.has(item.id) || seen.has(item.id)) continue
    seen.add(item.id)
    // item.id는 string이지만 knownIds에 있는 걸 확인했으니 WidgetId로 봐도 된다 (as: 타입 단언)
    merged.push({ id: item.id as WidgetId, size: item.size, hidden: item.hidden })
  }
  for (const item of defaultLayout) {
    if (!seen.has(item.id)) merged.push(item)
  }
  return merged
}

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
