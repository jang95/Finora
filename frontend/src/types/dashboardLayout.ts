// 통계 대시보드의 위젯 배치 (결정 D26, D27).
// 나중에 Backend DashboardLayouts 테이블의 Layout JSON과 같은 모양이 된다 (types 폴더 규칙: Backend DTO와 맞춘다).

// 위젯 id 목록. as const: 배열을 "바꿀 수 없는 고정 값"으로 만들어서,
// TypeScript가 string[]이 아니라 정확히 이 세 글자값의 배열로 본다.
// 새 위젯을 만들면 여기에 id를 더하고 components/statistics/widgets.tsx에 내용을 등록한다.
export const WIDGET_IDS = ['summary', 'monthlyTotals', 'categoryExpenses'] as const

// (typeof WIDGET_IDS)[number]: 배열의 "원소 타입" = 'summary' | 'monthlyTotals' | 'categoryExpenses'
// 목록에 id를 더하면 이 타입도 저절로 늘어난다 (두 곳을 따로 고칠 필요가 없다).
export type WidgetId = (typeof WIDGET_IDS)[number]

// 위젯 크기: PC(2열 격자)에서 반 칸 또는 한 줄 전체. 모바일은 1열이라 크기와 상관없이 세로로 쌓인다.
export type WidgetSize = 'half' | 'full'

// 배치 한 줄: 어떤 위젯을, 어떤 크기로, 숨겼는지. 배열의 순서가 화면 순서다.
export interface WidgetLayoutItem {
  id: WidgetId
  size: WidgetSize
  hidden: boolean
}
