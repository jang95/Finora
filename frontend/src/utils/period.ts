// 기간 계산. 대시보드·통계 화면의 기간 선택(PeriodPicker)이 같이 쓴다. (결정 D17)
// 날짜는 거래 date와 같은 'YYYY-MM-DD' 문자열로 다룬다. 그대로 API 주소(?from=&to=)에 넣을 수 있다.
import { toDateKey } from '@/utils/yearMonth'

// 날짜 하나로 정해지는 기간 단위 (그 날짜가 들어 있는 일/주/월/연)
export type FixedPeriodUnit = 'day' | 'week' | 'month' | 'year'

// 기간 단위. 직접 지정('custom')은 달력에서 시작·끝 날짜를 고른다.
// | 로 두 타입을 합친다 (유니온): FixedPeriodUnit 네 개 + 'custom'
export type PeriodUnit = FixedPeriodUnit | 'custom'

// 기간: from ~ to (양 끝 날짜 포함. Backend GET /api/statistics/summary와 같은 규칙)
export interface Period {
  unit: PeriodUnit
  from: string
  to: string
}

// date가 들어 있는 unit 단위 기간. 예) ('week', '2026-10-14') → 10월 12일(월) ~ 10월 18일(일)
// 직접 지정은 날짜 하나로 정할 수 없으므로 unit에 'custom'은 받지 않는다 (FixedPeriodUnit)
export function getPeriod(unit: FixedPeriodUnit, date: string): Period {
  const d = parseDate(date)
  switch (unit) {
    case 'day':
      return { unit, from: date, to: date }
    case 'week': {
      // getDay(): 0=일 ~ 6=토. 월요일까지 며칠 거슬러 갈지: 월 0, 화 1, ... 일 6
      const sinceMonday = (d.getDay() + 6) % 7
      const monday = addDays(d, -sinceMonday)
      return { unit, from: dateToKey(monday), to: dateToKey(addDays(monday, 6)) }
    }
    case 'month':
      // new Date(연, 다음 달, 0) = 이번 달 마지막 날 (utils/yearMonth.ts getDaysInMonth와 같은 방법)
      return { unit, from: dateToKey(new Date(d.getFullYear(), d.getMonth(), 1)), to: dateToKey(new Date(d.getFullYear(), d.getMonth() + 1, 0)) }
    case 'year':
      return { unit, from: `${d.getFullYear()}-01-01`, to: `${d.getFullYear()}-12-31` }
  }
}

// 같은 단위로 offset만큼 앞뒤로 이동한다 (-1: 이전, +1: 다음)
export function shiftPeriod(period: Period, offset: number): Period {
  const from = parseDate(period.from)
  switch (period.unit) {
    case 'day':
      return getPeriod('day', dateToKey(addDays(from, offset)))
    case 'week':
      return getPeriod('week', dateToKey(addDays(from, offset * 7)))
    case 'month':
      // from이 항상 1일이라 월을 더해도 31일 → 다음 달로 넘치는 문제가 없다
      return getPeriod('month', dateToKey(new Date(from.getFullYear(), from.getMonth() + offset, 1)))
    case 'year':
      return getPeriod('year', `${from.getFullYear() + offset}-01-01`)
    case 'custom': {
      // 직접 지정은 "그 기간의 날 수"만큼 이동한다. 예) 10/3~10/12(10일) → 다음: 10/13~10/22
      const days = daysBetween(from, parseDate(period.to)) + 1
      return { unit: 'custom', from: dateToKey(addDays(from, offset * days)), to: dateToKey(addDays(parseDate(period.to), offset * days)) }
    }
  }
}

// 직접 지정 기간. from이 to보다 늦으면 둘을 바꿔서 항상 from ≤ to가 되게 한다.
export function customPeriod(from: string, to: string): Period {
  return from <= to ? { unit: 'custom', from, to } : { unit: 'custom', from: to, to: from }
}

// 화면에 보여줄 기간 이름
export function formatPeriod({ unit, from, to }: Period): string {
  const f = parseDate(from)
  const t = parseDate(to)
  switch (unit) {
    case 'day':
      return `${f.getFullYear()}년 ${f.getMonth() + 1}월 ${f.getDate()}일 (${WEEKDAYS[f.getDay()]})`
    case 'week':
    case 'custom': {
      // case를 두 개 이어 쓰면 둘 다 같은 코드로 처리한다 (주간과 직접 지정은 둘 다 "시작 ~ 끝"으로 보여줌)
      if (from === to) return formatPeriod({ unit: 'day', from, to })
      // 해가 바뀌는 기간(12월 말 ~ 1월 초)은 끝 날짜에도 연도를 붙인다
      const end = f.getFullYear() === t.getFullYear() ? `${t.getMonth() + 1}월 ${t.getDate()}일` : `${t.getFullYear()}년 ${t.getMonth() + 1}월 ${t.getDate()}일`
      return `${f.getFullYear()}년 ${f.getMonth() + 1}월 ${f.getDate()}일 ~ ${end}`
    }
    case 'month':
      return `${f.getFullYear()}년 ${f.getMonth() + 1}월`
    case 'year':
      return `${f.getFullYear()}년`
  }
}

// ---- 날짜 변환 (달력 컴포넌트는 Date를 쓰므로 PeriodPicker에서도 쓴다) ----

// 'YYYY-MM-DD' → 로컬 날짜. new Date('2026-10-14')는 UTC로 해석돼 하루 밀릴 수 있어서 숫자로 나눈다 (utils/format.ts formatDate 참고)
export function parseDate(date: string): Date {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day)
}

// 로컬 날짜 → 'YYYY-MM-DD'
export function dateToKey(date: Date): string {
  return toDateKey({ year: date.getFullYear(), month: date.getMonth() + 1 }, date.getDate())
}

// ---- 내부 도우미 ----

const WEEKDAYS = ['일', '월', '화', '수', '목', '금', '토']

// 두 날짜 사이의 날 수 (같은 날이면 0).
// Date끼리 빼면 밀리초 차이가 나온다. 하루 = 86,400,000ms. Math.round: 서머타임 등으로 생길 수 있는 1시간 차이를 없앤다.
function daysBetween(from: Date, to: Date): number {
  return Math.round((to.getTime() - from.getTime()) / 86_400_000)
}

// 날짜 더하기. new Date(연, 월, 32)처럼 범위를 넘으면 Date가 다음 달로 알아서 넘겨준다
function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days)
}
