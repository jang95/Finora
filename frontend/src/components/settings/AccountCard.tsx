import { useState } from 'react'
import { useNavigate, useRouteLoaderData } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { logout } from '@/services/authService'
import type { Me } from '@/types/auth'

// 현재 로그인한 계정과 로그아웃 버튼
function AccountCard() {
  // useRouteLoaderData('main'): router.tsx에서 id가 'main'인 경로(MainLayout)의 loader 결과 = 현재 사용자.
  // 이 화면은 그 경로 안에서만 보이므로 값이 항상 있지만, 타입상 undefined일 수 있어 ?.로 꺼낸다.
  const me = useRouteLoaderData('main') as Me | undefined
  const navigate = useNavigate()
  const [submitting, setSubmitting] = useState(false)

  async function handleLogout() {
    setSubmitting(true)
    try {
      await logout()
    } finally {
      // 서버 요청이 실패해도 로그인 화면으로 보낸다. 거기서 loader가 로그인 상태를 다시 확인한다.
      navigate('/login', { replace: true })
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>계정</CardTitle>
        <CardDescription>{me?.email}</CardDescription>
        <CardAction>
          <Button variant="outline" size="sm" onClick={handleLogout} disabled={submitting}>
            로그아웃
          </Button>
        </CardAction>
      </CardHeader>
    </Card>
  )
}

export default AccountCard
