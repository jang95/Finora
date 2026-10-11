// 연·월 계산. 가계부·통계 화면이 같이 쓴다.

// 연·월을 한 묶음으로 다룬다. 따로 state 2개로 두면 12월 → 1월처럼 둘이 같이 바뀔 때 실수하기 쉽다.
export interface YearMonth {
  year: number
  month: number // 1~12
}

// 이번 달. getMonth()는 0~11이라 +1
export function getThisMonth(): YearMonth {
  const today = new Date()
  return { year: today.getFullYear(), month: today.getMonth() + 1 }
}

// offset만큼 월을 이동한다 (-1: 이전 달, +1: 다음 달). 연도 넘어가는 것도 처리한다.
export function addMonths({ year, month }: YearMonth, offset: number): YearMonth {
  // new Date(2026, 12, 1)처럼 범위를 넘는 월을 넣으면 Date가 알아서 2027년 1월로 바꿔준다
  const date = new Date(year, month - 1 + offset, 1)
  return { year: date.getFullYear(), month: date.getMonth() + 1 }
}

// 그달의 날짜 수. new Date(연, 다음 달, 0): "다음 달 0일" = 이번 달 마지막 날로 바뀌는 것을 이용한다 (month가 1~12라 그대로 넣으면 다음 달)
export function getDaysInMonth({ year, month }: YearMonth): number {
  return new Date(year, month, 0).getDate()
}

// 그달 1일의 요일. 0=일요일 ~ 6=토요일 (달력 첫 줄에서 1일 앞에 빈칸을 몇 개 둘지 정한다)
export function getFirstWeekday({ year, month }: YearMonth): number {
  return new Date(year, month - 1, 1).getDay()
}

// 'YYYY-MM-DD' 문자열. 거래의 date와 같은 형식이라 서로 비교할 수 있다.
export function toDateKey({ year, month }: YearMonth, day: number): string {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

// 오늘 'YYYY-MM-DD'. toISOString()은 UTC 기준이라 한국 오전 9시 전에는 어제 날짜가 나오므로 직접 만든다.
export function getToday(): string {
  const today = new Date()
  return toDateKey({ year: today.getFullYear(), month: today.getMonth() + 1 }, today.getDate())
}
