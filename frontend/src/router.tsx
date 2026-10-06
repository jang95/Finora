import { createBrowserRouter } from 'react-router'
import AuthLayout from './layouts/AuthLayout'
import MainLayout from './layouts/MainLayout'
import AssetsPage from './pages/AssetsPage'
import DashboardPage from './pages/DashboardPage'
import InvestmentsPage from './pages/InvestmentsPage'
import LoginPage from './pages/LoginPage'
import NotFoundPage from './pages/NotFoundPage'
import RegisterPage from './pages/RegisterPage'
import SettingsPage from './pages/SettingsPage'
import StatisticsPage from './pages/StatisticsPage'
import TransactionsPage from './pages/TransactionsPage'

// 주소(path)와 화면(Component)의 연결표.
// children은 부모 레이아웃의 <Outlet /> 자리에 들어간다.
export const router = createBrowserRouter([
  {
    Component: AuthLayout,
    children: [
      { path: '/login', Component: LoginPage },
      { path: '/register', Component: RegisterPage },
    ],
  },
  {
    Component: MainLayout,
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
