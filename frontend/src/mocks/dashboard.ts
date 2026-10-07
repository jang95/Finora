import type { DashboardSummary } from '@/types/dashboard'

// Phase 2용 가짜 데이터. services/만 import한다. (실제 금융 데이터를 넣지 않는다)
export const dashboardSummaryMock: DashboardSummary = {
  totalAssets: 52_340_000,
  monthlyIncome: 3_800_000,
  monthlyExpense: 2_145_500,
  investment: {
    totalCost: 10_000_000,
    totalValue: 10_325_000,
    profit: 325_000,
    profitRate: 3.25,
  },
}
