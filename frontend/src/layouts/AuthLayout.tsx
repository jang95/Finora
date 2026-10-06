import { Outlet } from 'react-router'

// 로그인·회원가입 화면의 골격. 메뉴 없이 가운데에 폼만 보여준다.
function AuthLayout() {
  return (
    <main>
      <Outlet />
    </main>
  )
}

export default AuthLayout
