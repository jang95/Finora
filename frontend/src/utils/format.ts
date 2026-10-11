// 화면 표시용 포맷 함수. 값을 문자열로 바꾸기만 하고 금액 계산은 하지 않는다.

// Intl.NumberFormat: 브라우저 내장 숫자 포맷터. 'ko-KR'이면 3자리마다 , 를 넣는다.
// 만들 때 비용이 있어서 함수 밖에서 한 번만 만들고 재사용한다.
const moneyFormatter = new Intl.NumberFormat('ko-KR', { maximumFractionDigits: 0 })

// signDisplay: 'exceptZero' → 양수는 +, 음수는 -, 0은 부호 없이 표시
const signedMoneyFormatter = new Intl.NumberFormat('ko-KR', {
  maximumFractionDigits: 0,
  signDisplay: 'exceptZero',
})

const percentFormatter = new Intl.NumberFormat('ko-KR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  signDisplay: 'exceptZero',
})

// 52340000 → "52,340,000원"  (signed: true이면 "+325,000원")
export function formatMoney(amount: number, signed = false): string {
  const formatter = signed ? signedMoneyFormatter : moneyFormatter
  return `${formatter.format(amount)}원`
}

// weekday: 'short' → 한국어 요일 한 글자 (월, 화 ...)
const dateFormatter = new Intl.DateTimeFormat('ko-KR', {
  month: 'long',
  day: 'numeric',
  weekday: 'short',
})

// '2026-10-07' → "10월 7일 (수)"
// new Date('2026-10-07')은 UTC 자정으로 해석돼서 시간대에 따라 날짜가 하루 밀릴 수 있다.
// 그래서 숫자로 나눠서 로컬 날짜로 만든다. (month는 0부터 시작해서 -1)
export function formatDate(date: string): string {
  const [year, month, day] = date.split('-').map(Number)
  return dateFormatter.format(new Date(year, month - 1, day))
}

// 3.25 → "+3.25%"
export function formatPercent(value: number): string {
  return `${percentFormatter.format(value)}%`
}

// notation: 'compact' → 큰 수를 짧게. 'ko-KR'이면 만/억 단위를 쓴다. 차트 축처럼 자리가 좁은 곳에 쓴다.
const compactMoneyFormatter = new Intl.NumberFormat('ko-KR', {
  notation: 'compact',
  maximumFractionDigits: 1,
})

// 3800000 → "380만", 52340000 → "5234만"
export function formatMoneyCompact(amount: number): string {
  return compactMoneyFormatter.format(amount)
}

const ratioFormatter = new Intl.NumberFormat('ko-KR', { maximumFractionDigits: 1 })

// 비중 표시. 부호 없이 소수 첫째 자리까지. 35.4821 → "35.5%"
export function formatRatio(value: number): string {
  return `${ratioFormatter.format(value)}%`
}

// 원 없이 숫자만. 달력 칸처럼 좁은 곳에 쓴다. 3800000 → "3,800,000"
export function formatAmount(amount: number): string {
  return moneyFormatter.format(amount)
}
