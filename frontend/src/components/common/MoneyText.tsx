import { cn } from '@/lib/utils'
import { formatMoney } from '@/utils/format'

// tone: 금액 색상
// - 'income' / 'expense': 항상 수입(초록)/지출(빨강) 색
// - 'profit': 투자 손익. 한국식으로 양수는 gain(빨강), 음수는 loss(파랑) 색 + 부호(+/-) 표시 (결정 D9)
// - 지정하지 않으면 기본 글자색
type MoneyTone = 'income' | 'expense' | 'profit'

// [props] 부모 컴포넌트가 넘겨주는 값. C# 메서드의 매개변수와 비슷하다.
//   <MoneyText amount={1000} tone="income" />
//   → MoneyText({ amount: 1000, tone: 'income' }) 처럼 객체 하나로 전달된다.
// - JSX에서 문자열은 "..."로, 숫자·변수·식은 {...}로 넘긴다.
// - ?가 붙은 속성은 생략 가능하다. 생략하면 undefined가 된다.
// - props는 읽기만 한다. 자식이 바꾸면 안 되고, 바꿔야 하는 값은 부모의 state로 둔다.
// - 받을 수 있는 props를 interface로 정해두면 잘못된 이름·타입을 넘겼을 때 에러가 나고, 자동완성도 된다.
interface MoneyTextProps {
  amount: number
  tone?: MoneyTone
  className?: string // 사용하는 쪽에서 크기 등 스타일을 덧붙일 때 (cn으로 합친다)
}

// 매개변수 자리의 { amount, tone, className }는 구조 분해: props.amount 대신 amount로 바로 쓴다
function MoneyText({ amount, tone, className }: MoneyTextProps) {
  const isProfit = tone === 'profit'

  return (
    // tabular-nums: 숫자 폭을 같게 맞춰서 금액이 위아래로 정렬되게 한다
    <span
      className={cn(
        'tabular-nums',
        tone === 'income' && 'text-income',
        tone === 'expense' && 'text-expense',
        isProfit && amount > 0 && 'text-gain',
        isProfit && amount < 0 && 'text-loss',
        className,
      )}
    >
      {formatMoney(amount, isProfit)}
    </span>
  )
}

export default MoneyText
