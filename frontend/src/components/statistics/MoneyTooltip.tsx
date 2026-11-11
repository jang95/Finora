import MoneyText from '@/components/common/MoneyText'
import { ChartTooltip, ChartTooltipContent, type ChartConfig } from '@/components/ui/chart'

interface MoneyTooltipProps {
  config: ChartConfig
  // 툴팁 제목(가로축 값)을 보여줄 방법. 없으면 데이터의 label 그대로
  labelFormatter?: (label: unknown) => string
}

// 금액 차트 공통 툴팁: 줄마다 "색 표시 · 이름 · 금액(원)". 여러 위젯이 같은 모양을 쓰려고 분리했다.
// shadcn 기본 툴팁은 '원'이 없고 소수가 그대로 보여서 MoneyText로 바꾼다 (MonthlyTotalsWidget의 formatter와 같은 이유).
// 색은 ChartContainer가 config에서 만들어 주는 CSS 변수(--color-<키>)를 쓴다.
function MoneyTooltip({ config, labelFormatter }: MoneyTooltipProps) {
  return (
    <ChartTooltip
      content={
        <ChartTooltipContent
          labelFormatter={labelFormatter ? (label) => labelFormatter(label) : undefined}
          formatter={(value, name) => (
            <div className="flex w-full items-center gap-2">
              {/* name은 <Line dataKey>/<Area dataKey> 값. config에서 이름표를 찾고, 색은 같은 키의 CSS 변수 */}
              <span className="size-2.5 shrink-0 rounded-[2px]" style={{ backgroundColor: `var(--color-${String(name)})` }} />
              <span className="text-muted-foreground">{config[String(name)]?.label ?? String(name)}</span>
              <MoneyText amount={Number(value)} className="ml-auto font-medium" />
            </div>
          )}
        />
      }
    />
  )
}

export default MoneyTooltip
