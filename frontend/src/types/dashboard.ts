// 대시보드 요약 데이터. Backend가 계산해서 내려주는 응답(DTO)과 이름·형태를 맞춘다.
// 금액은 Backend decimal → JSON number로 온다. Frontend는 표시만 하고 더하기 등 계산은 하지 않는다. (결정 D4)
// 투자 요약은 Phase 10(투자)에서 추가한다. (결정 D10)

export interface DashboardSummary {
  totalAssets: number
  // 이번 달 수입/지출 (이체는 포함하지 않음)
  monthlyIncome: number
  monthlyExpense: number
}
