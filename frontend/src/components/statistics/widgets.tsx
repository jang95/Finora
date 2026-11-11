import type { ComponentType } from 'react'
import CategoryDonutWidget from '@/components/statistics/CategoryDonutWidget'
import CategoryExpenseWidget from '@/components/statistics/CategoryExpenseWidget'
import CategoryMonthlyWidget from '@/components/statistics/CategoryMonthlyWidget'
import CumulativeCompareWidget from '@/components/statistics/CumulativeCompareWidget'
import DailyExpenseWidget from '@/components/statistics/DailyExpenseWidget'
import MonthlyTotalsWidget from '@/components/statistics/MonthlyTotalsWidget'
import PeriodSummaryWidget from '@/components/statistics/PeriodSummaryWidget'
import SavingRateWidget from '@/components/statistics/SavingRateWidget'
import TopExpensesWidget from '@/components/statistics/TopExpensesWidget'
import TotalAssetsWidget from '@/components/statistics/TotalAssetsWidget'
import WeekdayAverageWidget from '@/components/statistics/WeekdayAverageWidget'
import { WIDGET_IDS, type WidgetId, type WidgetLayoutItem, type WidgetSize } from '@/types/dashboardLayout'
import type { Period } from '@/utils/period'

// 위젯 목록(registry). 통계 대시보드가 "어떤 위젯이 있고, 무엇을 그리는지"를 여기서 찾는다. (결정 D26)
// 새 위젯 추가 = types/dashboardLayout.ts의 WIDGET_IDS에 id 추가 + 여기 한 줄 등록.

// 모든 위젯이 받는 props. 기간은 화면 위의 PeriodPicker 하나를 모두가 같이 쓴다.
export interface WidgetProps {
  period: Period
}

interface WidgetDefinition {
  name: string
  defaultSize: WidgetSize
  // [ComponentType] "props가 WidgetProps인 컴포넌트"라는 타입. 함수 컴포넌트를 값처럼 변수에 담아 두고
  //   나중에 <Component period={...} />로 그릴 수 있다 (StatisticsDashboard 참고).
  //   C#으로 치면 "이 시그니처를 가진 메서드"를 delegate 변수에 담아 두는 것과 비슷하다.
  component: ComponentType<WidgetProps>
}

// [Record<K, V>] "K의 모든 값을 키로 가지는 객체" 타입. WidgetId 셋 중 하나라도 빠지면 TypeScript가 에러를 낸다.
//   그래서 WIDGET_IDS에 id를 더하고 여기 등록을 잊으면 바로 알 수 있다.
export const WIDGETS: Record<WidgetId, WidgetDefinition> = {
  summary: { name: '기간 요약', defaultSize: 'full', component: PeriodSummaryWidget },
  monthlyTotals: { name: '월별 수입·지출', defaultSize: 'half', component: MonthlyTotalsWidget },
  categoryExpenses: { name: '카테고리별 지출', defaultSize: 'half', component: CategoryExpenseWidget },
  dailyExpense: { name: '일별 지출 추이', defaultSize: 'half', component: DailyExpenseWidget },
  cumulativeCompare: { name: '이전 기간 누적 비교', defaultSize: 'half', component: CumulativeCompareWidget },
  categoryDonut: { name: '카테고리 비중', defaultSize: 'half', component: CategoryDonutWidget },
  categoryMonthly: { name: '상위 카테고리 월별 추이', defaultSize: 'full', component: CategoryMonthlyWidget },
  weekdayAverage: { name: '요일별 평균 지출', defaultSize: 'half', component: WeekdayAverageWidget },
  topExpenses: { name: '큰 지출 TOP 5', defaultSize: 'half', component: TopExpensesWidget },
  savingRate: { name: '저축률 추이', defaultSize: 'half', component: SavingRateWidget },
  totalAssets: { name: '총자산 추이', defaultSize: 'half', component: TotalAssetsWidget },
}

// 기본 배치: 위젯 목록 순서대로, 기본 크기로, 모두 보임. 저장된 배치가 없을 때 쓴다 (결정 D27).
export const DEFAULT_LAYOUT: WidgetLayoutItem[] = WIDGET_IDS.map((id) => ({
  id,
  size: WIDGETS[id].defaultSize,
  hidden: false,
}))
