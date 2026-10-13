import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { getServerStatus } from '@/services/healthService'
import type { ServerStatus } from '@/types/health'

// Record<키, 값>: 모든 ServerStatus 값마다 문구가 있어야 한다. 상태가 추가되면 여기서 컴파일 오류로 알려 준다.
const statusText: Record<ServerStatus, { label: string; className: string }> = {
  ok: { label: '정상', className: 'text-foreground' },
  dbDown: { label: '서버는 응답하지만 DB에 연결되지 않습니다', className: 'text-destructive' },
  serverDown: { label: '서버에 연결할 수 없습니다', className: 'text-destructive' },
}

// Backend·DB 연결 상태 (GET /api/health)
function ServerStatusCard() {
  // null = 확인 중
  const [status, setStatus] = useState<ServerStatus | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  // 데이터 불러오기 방식은 DashboardPage 설명 참고
  useEffect(() => {
    let ignore = false

    getServerStatus().then((data) => {
      if (!ignore) setStatus(data)
    })

    return () => {
      ignore = true
    }
  }, [reloadKey])

  function recheck() {
    setStatus(null)
    setReloadKey((prev) => prev + 1)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>서버 상태</CardTitle>
        <CardDescription>Backend와 Database 연결을 확인합니다.</CardDescription>
        <CardAction>
          <Button variant="outline" size="sm" onClick={recheck} disabled={status === null}>
            다시 확인
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent>
        {status === null ? (
          <p className="text-muted-foreground">확인 중...</p>
        ) : (
          <p className={statusText[status].className}>{statusText[status].label}</p>
        )}
      </CardContent>
    </Card>
  )
}

export default ServerStatusCard
