import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

interface WidgetCardProps {
  title: string
  description?: string
  // 이전 기간 데이터를 보여주는 중(keepPreviousData)이면 흐리게
  isStale?: boolean
  // 불러오기 실패. 있으면 내용 대신 오류 문구를 보여준다
  error?: Error | null
  // 데이터가 아직 없으면 "불러오는 중..."
  isLoading?: boolean
  children: ReactNode
}

// 통계 위젯의 공통 껍데기: 제목·설명 + 불러오는 중 / 오류 / 내용.
// 위젯마다 같은 세 가지 상태 처리를 반복하지 않으려고 한곳에 모았다.
// 기간 요약 위젯처럼 제목이 없는 위젯은 이 껍데기를 쓰지 않고 직접 그린다.
function WidgetCard({ title, description, isStale, error, isLoading, children }: WidgetCardProps) {
  return (
    // h-full: 격자에서 옆 칸 위젯과 높이를 맞춘다 (한 줄에 둘이 나란히 올 때 아래가 비지 않게)
    <Card className={cn('h-full', isStale && 'opacity-60')}>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {error ? (
          <p role="alert" className="text-destructive">
            {error.message}
          </p>
        ) : isLoading ? (
          <p className="text-muted-foreground">불러오는 중...</p>
        ) : (
          children
        )}
      </CardContent>
    </Card>
  )
}

export default WidgetCard
