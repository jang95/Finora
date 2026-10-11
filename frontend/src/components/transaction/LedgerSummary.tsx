import type { ReactNode } from 'react'
import MoneyText from '@/components/common/MoneyText'
import { Card } from '@/components/ui/card'

interface LedgerSummaryProps {
  income: number
  expense: number
  balance: number
}

// 기간 요약: 수입 / 지출 / 합계 세 칸. 가계부 월 화면과 월별(연간) 화면에서 같이 쓴다.
// 합계 계산은 하지 않는다. service(나중엔 Backend)가 준 값을 그대로 보여준다 (결정 D4)
function LedgerSummary({ income, expense, balance }: LedgerSummaryProps) {
  return (
    // grid-cols-3 + divide-x: 세 칸을 나란히 두고 칸 사이에만 세로선
    <Card className="grid grid-cols-3 gap-0 divide-x py-0 text-center">
      <SummaryCell label="수입">
        <MoneyText amount={income} tone="income" />
      </SummaryCell>
      <SummaryCell label="지출">
        <MoneyText amount={expense} tone="expense" />
      </SummaryCell>
      <SummaryCell label="합계">
        <MoneyText amount={balance} signed />
      </SummaryCell>
    </Card>
  )
}

// [children] 여는 태그와 닫는 태그 사이에 넣은 내용이 children prop으로 들어온다.
//   <SummaryCell label="수입"><MoneyText .../></SummaryCell> → children = <MoneyText .../>
//   ReactNode: 화면에 그릴 수 있는 모든 것(요소, 문자열, 숫자, null 등)의 타입
function SummaryCell({ label, children }: { label: string; children: ReactNode }) {
  return (
    // min-w-0 + break-all: 모바일에서 큰 금액이 칸을 넘치지 않고 줄바꿈되게 한다
    <div className="flex min-w-0 flex-col gap-1 px-2 py-3">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="text-sm font-semibold break-all sm:text-base">{children}</span>
    </div>
  )
}

export default LedgerSummary
