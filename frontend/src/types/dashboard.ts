import type { Transaction } from '@/types/transaction'

// 대시보드 데이터 (GET /api/dashboard). Backend 응답(DTO)과 이름·형태를 맞춘다.
// 기간과 상관없는 정보만 담는다. 기간별 수입·지출·총자산은 PeriodSummary(GET /api/statistics/summary)를 쓴다.
// 계좌 기준 총자산은 Phase 8, 투자 요약은 Phase 10에서 추가한다. (결정 D10, D17)
// 구조를 바꿀 때는 이 타입부터 고친다. 그러면 TypeScript가 이 값을 쓰는 화면에서 에러를 내서 고칠 곳을 빠짐없이 알려준다.

export interface DashboardSummary {
  // 최근 거래 몇 건 (최신이 위)
  recentTransactions: Transaction[]
}
