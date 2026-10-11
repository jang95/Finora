import { Link } from 'react-router'
import { Button } from '@/components/ui/button'

// 없는 주소로 들어왔을 때. 레이아웃 밖이라 메뉴가 없으므로 돌아갈 링크를 둔다.
function NotFoundPage() {
  return (
    // min-h-svh: 화면 높이만큼 채워서 가운데 정렬한다 (svh: 모바일 주소창 높이를 뺀 실제 보이는 높이)
    <div className="flex min-h-svh flex-col items-center justify-center gap-4 bg-background p-4 text-center">
      <h1 className="text-2xl font-bold">페이지를 찾을 수 없습니다</h1>
      <p className="text-muted-foreground">주소가 바뀌었거나 없는 페이지입니다.</p>
      {/* asChild 설명은 TransactionFormDialog 참고. 버튼 모양의 Link가 된다 */}
      <Button asChild>
        <Link to="/">대시보드로 가기</Link>
      </Button>
    </div>
  )
}

export default NotFoundPage
