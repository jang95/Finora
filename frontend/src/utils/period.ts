// 기간 계산. 대시보드·통계 화면의 기간 선택(PeriodPicker)이 같이 쓴다. (결정 D17)
// 날짜는 거래 date와 같은 'YYYY-MM-DD' 문자열로 다룬다. 그대로 API 주소(?from=&to=)에 넣을 수 있다.
import { toDateKey } from '@/utils/yearMonth'

// 기간 단위. 직접 지정('custom')은 달력으로 고른다 (다음 단계에서 추가)
export type PeriodUnit = 'day' | 'week' | 'month' | 'year'

// 기간: from ~ to (양 끝 날짜 포함. Backend GET /api/statistics/summary와 같은 규칙)
export interface Period {
  unit: PeriodUnit
  from: string
  to: string
}

// date가 들어 있는 unit 단위 기간. 예) ('week', '2026-10-14') → 10월 12일(월) ~ 10월 18일(일)
export function getPeriod(unit: PeriodUnit, date: string): Period {
  const d = parseDate(date)
  switch (unit) {
    case 'day':
      return { unit, from: date, to: date }
    case 'week': {
      // getDay(): 0=일 ~ 6=토. 월요일까지 며칠 거슬러 갈지: 월 0, 화 1, ... 일 6
      const sinceMonday = (d.getDay() + 6) % 7
      const monday = addDays(d, -sinceMonday)
      return { unit, from: toKey(monday), to: toKey(addDays(monday, 6)) }
    }
    case 'month':
      // new Date(연, 다음 달, 0) = 이번 달 마지막 날 (utils/yearMonth.ts getDaysInMonth와 같은 방법)
      return { unit, from: toKey(new Date(d.getFullYear(), d.getMonth(), 1)), to: toKey(new Date(d.getFullYear(), d.getMonth() + 1, 0)) }
    case 'year':
      return { unit, from: `${d.getFullYear()}-01-01`, to: `${d.getFullYear()}-12-31` }
  }
}

// 같은 단위로 offset만큼 앞뒤로 이동한다 (-1: 이전, +1: 다음)
export function shiftPeriod(period: Period, offset: number): Period {
  const from = parseDate(period.from)
  switch (period.unit) {
    case 'day':
      return getPeriod('day', toKey(addDays(from, offset)))
    case 'week':
      return getPeriod('week', toKey(addDays(from, offset * 7)))
    case 'month':
      // from이 항상 1일이라 월을 더해도 31일 → 다음 달로 넘치는 문제가 없다
      return getPeriod('month', toKey(new Date(from.getFullYear(), from.getMonth() + offset, 1)))
    case 'year':
      return getPeriod('year', `${from.getFullYear() + offset}-01-01`)
  }
}

// 화면에 보여줄 기간 이름
export function formatPeriod({ unit, from, to }: Period): string {
  const f = parseDate(from)
  const t = parseDate(to)
  switch (unit) {
    case 'day':
      return `${f.getFullYear()}년 ${f.getMonth() + 1}월 ${f.getDate()}일 (${WEEKDAYS[f.getDay()]})`
    case 'week': {
      // 해가 바뀌는 주(12월 말 ~ 1월 초)는 끝 날짜에도 연도를 붙인다
      const end = f.getFullYear() === t.getFullYear() ? `${t.getMonth() + 1}월 ${t.getDate()}일` : `${t.getFullYear()}년 ${t.getMonth() + 1}월 ${t.getDate()}일`
      return `${f.getFullYear()}년 ${f.getMonth() + 1}월 ${f.getDate()}일 ~ ${end}`
    }
    case 'month':
      return `${f.getFullYear()}년 ${f.getMonth() + 1}월`
    case 'year':
      return `${f.getFullYear()}년`
  }
}

// ---- 내부 도우미 ----

const WEEKDAYS = ['일', '월', '화', '수', '목', '금', '토']

// 'YYYY-MM-DD' → 로컬 날짜. new Date('2026-10-14')는 UTC로 해석돼 하루 밀릴 수 있어서 숫자로 나눈다 (utils/format.ts formatDate 참고)
function parseDate(date: string): Date {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day)
}

function toKey(date: Date): string {
  return toDateKey({ year: date.getFullYear(), month: date.getMonth() + 1 }, date.getDate())
}

// 날짜 더하기. new Date(연, 월, 32)처럼 범위를 넘으면 Date가 다음 달로 알아서 넘겨준다
function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days)
}
