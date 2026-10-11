import type { Transaction } from '@/types/transaction'

// 대시보드 요약 데이터. Backend가 계산해서 내려주는 응답(DTO)과 이름·형태를 맞춘다.
// 금액은 Backend decimal → JSON number로 온다. Frontend는 표시만 하고 더하기 등 계산은 하지 않는다. (결정 D4)
// 투자 요약은 Phase 10(투자)에서 추가한다. (결정 D10)
// 구조를 바꿀 때는 이 타입부터 고친다. 그러면 TypeScript가 이 값을 쓰는 mock·화면에서 에러를 내서 고칠 곳을 빠짐없이 알려준다.

export interface DashboardSummary {
  totalAssets: number
  // 이번 달 수입/지출 (이체는 포함하지 않음)
  monthlyIncome: number
  monthlyExpense: number
  // 최근 거래 몇 건 (최신이 위)
  recentTransactions: Transaction[]
}
