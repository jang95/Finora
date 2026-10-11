// 연·월 계산. 가계부·통계 화면이 같이 쓴다.

// 연·월을 한 묶음으로 다룬다. 따로 state 2개로 두면 12월 → 1월처럼 둘이 같이 바뀔 때 실수하기 쉽다.
export interface YearMonth {
  year: number
  month: number // 1~12
}

// 이번 달. getMonth()는 0~11이라 +1
export function getThisMonth(): YearMonth {
  const today = new Date()
  return { year: today.getFullYear(), month: today.getMonth() + 1 }
}

// offset만큼 월을 이동한다 (-1: 이전 달, +1: 다음 달). 연도 넘어가는 것도 처리한다.
export function addMonths({ year, month }: YearMonth, offset: number): YearMonth {
  // new Date(2026, 12, 1)처럼 범위를 넘는 월을 넣으면 Date가 알아서 2027년 1월로 바꿔준다
  const date = new Date(year, month - 1 + offset, 1)
  return { year: date.getFullYear(), month: date.getMonth() + 1 }
}
