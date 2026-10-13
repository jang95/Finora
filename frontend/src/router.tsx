import { createBrowserRouter, redirect } from 'react-router'
import PageLoading from './components/common/PageLoading'
import AuthLayout from './layouts/AuthLayout'
import MainLayout from './layouts/MainLayout'
import AssetsPage from './pages/AssetsPage'
import DashboardPage from './pages/DashboardPage'
import ErrorPage from './pages/ErrorPage'
import InvestmentsPage from './pages/InvestmentsPage'
import LoginPage from './pages/LoginPage'
import NotFoundPage from './pages/NotFoundPage'
import RegisterPage from './pages/RegisterPage'
import SettingsPage from './pages/SettingsPage'
import StatisticsPage from './pages/StatisticsPage'
import TransactionsPage from './pages/TransactionsPage'
import { getMe } from './services/authService'

// [loader] 화면을 그리기 "전에" React Router가 먼저 실행하는 함수.
// 여기서 redirect를 throw하면 화면을 그리지 않고 다른 주소로 보낸다. return한 값은 화면에서 useLoaderData()로 꺼낸다.

// 로그인 후 화면: 로그인하지 않았으면 로그인 화면으로 보낸다.
// 진짜 보안은 Backend가 지킨다(로그인 안 하면 API가 401). 이 검사는 화면을 보여줄지 정하는 편의 기능이다.
async function requireLogin() {
  const me = await getMe()
  if (me === null) throw redirect('/login')
  return me
}

// 로그인·가입 화면: 이미 로그인했으면 대시보드로 보낸다.
// 서버 오류로 확인하지 못해도 로그인 화면은 보여준다. (.catch로 오류를 null로 바꿈)
async function redirectIfLoggedIn() {
  const me = await getMe().catch(() => null)
  if (me !== null) throw redirect('/')
  return null
}

// 주소(path)와 화면(Component)의 연결표.
// children은 부모 레이아웃의 <Outlet /> 자리에 들어간다.
// ErrorBoundary: loader나 화면에서 예상하지 못한 오류(서버 꺼짐 등)가 나면 대신 보여줄 화면.
export const router = createBrowserRouter([
  {
    Component: AuthLayout,
    loader: redirectIfLoggedIn,
    HydrateFallback: PageLoading,
    children: [
      { path: '/login', Component: LoginPage },
      { path: '/register', Component: RegisterPage },
    ],
  },
  {
    // id: 다른 화면에서 useRouteLoaderData('main')으로 이 loader의 결과(현재 사용자)를 꺼낼 때 쓰는 이름
    id: 'main',
    Component: MainLayout,
    loader: requireLogin,
    HydrateFallback: PageLoading,
    ErrorBoundary: ErrorPage,
    children: [
      { path: '/', Component: DashboardPage },
      { path: '/transactions', Component: TransactionsPage },
      { path: '/assets', Component: AssetsPage },
      { path: '/investments', Component: InvestmentsPage },
      { path: '/statistics', Component: StatisticsPage },
      { path: '/settings', Component: SettingsPage },
    ],
  },
  { path: '*', Component: NotFoundPage },
])
