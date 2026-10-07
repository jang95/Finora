import { Outlet } from 'react-router'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

// 로그인·회원가입 화면의 골격. 메뉴 없이 가운데 카드 안에 폼만 보여준다.
function AuthLayout() {
  return (
    <main className="flex min-h-svh items-center justify-center bg-muted p-4">
      <Card className="w-full max-w-sm py-8">
        <CardHeader className="px-8">
          <CardTitle className="text-center text-2xl font-bold tracking-wider">
            FINORA
          </CardTitle>
        </CardHeader>
        <CardContent className="px-8">
          <Outlet />
        </CardContent>
      </Card>
    </main>
  )
}

export default AuthLayout
