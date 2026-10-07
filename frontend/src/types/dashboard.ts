// 대시보드 요약 데이터. Backend가 계산해서 내려주는 응답(DTO)과 이름·형태를 맞춘다.
// 금액은 Backend decimal → JSON number로 온다. Frontend는 표시만 하고 더하기 등 계산은 하지 않는다. (결정 D4)

export interface DashboardSummary {
  totalAssets: number
  // 이번 달 수입/지출 (이체는 포함하지 않음)
  monthlyIncome: number
  monthlyExpense: number
  investment: InvestmentSummary
}

export interface InvestmentSummary {
  totalCost: number // 매수 원금
  totalValue: number // 평가금액
  profit: number // 평가손익 (손실이면 음수)
  profitRate: number // 수익률(%) 예: 3.25
}
