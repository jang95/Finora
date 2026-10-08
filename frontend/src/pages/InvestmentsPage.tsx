import { Hourglass } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'

// 투자 기능은 첫 배포 이후(Phase 10)에 만든다. 그때까지 메뉴는 남겨두고 안내만 보여준다. (결정 D10)
function InvestmentsPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">투자</h1>
      <Card>
        <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
          {/* aria-hidden: 꾸밈용 아이콘이라 화면낭독기가 읽지 않게 한다 */}
          <Hourglass className="size-10 text-muted-foreground" aria-hidden />
          <p className="font-medium">투자 관리는 준비 중입니다.</p>
          <p className="text-sm text-muted-foreground">가계부와 자산 기능을 먼저 제공하고, 이후에 추가될 예정입니다.</p>
        </CardContent>
      </Card>
    </div>
  )
}

export default InvestmentsPage
