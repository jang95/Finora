import type { ChartConfig } from '@/components/ui/chart'

// 카테고리 묶음(상위 N개 + 기타)에 차트 색을 붙이는 공통 규칙. 도넛(⑥)과 쌓은 막대(⑦)가 같이 쓴다.
// 색은 1번부터 차례로, 순서를 바꾸지 않는다 (index.css chart-1~5 설명). "기타"(categoryId null)는 회색.

// Recharts의 dataKey로 쓸 이름. 카테고리 이름은 '카페·간식'처럼 기호가 섞일 수 있어서 s0, s1 같은 짧은 키를 따로 만든다
export function seriesKey(index: number): string {
  return `s${index}`
}

// index번째 묶음의 색. 기타는 테마의 muted-foreground(회색)
export function seriesColor(index: number, isOther: boolean): string {
  return isOther ? 'var(--muted-foreground)' : `var(--chart-${index + 1})`
}

// 묶음 목록 → shadcn ChartConfig ({ s0: { label: '식비', color: ... }, ... }).
// Object.fromEntries: [키, 값] 쌍의 배열을 객체로 만든다 (Object.entries의 반대)
export function buildSeriesConfig(series: { categoryId: number | null; name: string }[]): ChartConfig {
  return Object.fromEntries(
    series.map((s, i) => [seriesKey(i), { label: s.name, color: seriesColor(i, s.categoryId === null) }]),
  )
}
