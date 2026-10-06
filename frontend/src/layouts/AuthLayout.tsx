import { Outlet } from 'react-router'

// 로그인·회원가입 화면의 골격. 메뉴 없이 가운데 카드 안에 폼만 보여준다.
function AuthLayout() {
  return (
    <main className="flex min-h-svh items-center justify-center bg-muted p-4">
      <div className="w-full max-w-sm rounded-xl border bg-card p-8 text-card-foreground shadow-sm">
        <strong className="mb-4 block text-center text-2xl font-bold tracking-wider">
          FINORA
        </strong>
        <Outlet />
      </div>
    </main>
  )
}

export default AuthLayout
