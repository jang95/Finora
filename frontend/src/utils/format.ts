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

// 3.25 → "+3.25%"
export function formatPercent(value: number): string {
  return `${percentFormatter.format(value)}%`
}
