import { Hourglass } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'

interface ComingSoonProps {
  title: string
  description: string
}

// 아직 만들지 않은 메뉴의 "준비 중" 안내 (투자, 자산)
function ComingSoon({ title, description }: ComingSoonProps) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
        {/* aria-hidden: 꾸밈용 아이콘이라 화면낭독기가 읽지 않게 한다 */}
        <Hourglass className="size-10 text-muted-foreground" aria-hidden />
        <p className="font-medium">{title}</p>
        <p className="text-sm text-muted-foreground">{description}</p>
      </CardContent>
    </Card>
  )
}

export default ComingSoon
