import {
  ChartPie,
  LayoutDashboard,
  Landmark,
  ReceiptText,
  Settings,
  TrendingUp,
} from 'lucide-react'
import { NavLink, Outlet } from 'react-router'
import { cn } from '@/lib/utils'

const menus = [
  { to: '/', label: '대시보드', icon: LayoutDashboard },
  { to: '/transactions', label: '가계부', icon: ReceiptText },
  { to: '/assets', label: '자산', icon: Landmark },
  { to: '/investments', label: '투자', icon: TrendingUp },
  { to: '/statistics', label: '통계', icon: ChartPie },
  { to: '/settings', label: '설정', icon: Settings },
]

// 로그인 후 화면의 공통 골격. 메뉴는 고정, <Outlet />에 현재 주소의 페이지가 들어간다.
// 모바일: 하단 탭 / md(768px) 이상: 왼쪽 사이드 메뉴
function MainLayout() {
  return (
    <div className="flex min-h-svh">
      <nav className="fixed inset-x-0 bottom-0 z-10 border-t bg-sidebar md:sticky md:top-0 md:h-svh md:w-56 md:shrink-0 md:border-t-0 md:border-r md:px-3 md:py-6">
        <strong className="mx-3 mb-6 hidden text-xl font-bold tracking-wider md:block">
          FINORA
        </strong>
        <ul className="flex md:flex-col md:gap-1">
          {menus.map(({ to, label, icon: Icon }) => (
            <li key={to} className="flex-1">
              <NavLink
                to={to}
                end={to === '/'}
                className={({ isActive }) =>
                  cn(
                    'flex flex-col items-center gap-1 py-2 text-xs md:flex-row md:gap-3 md:rounded-md md:px-3 md:py-2.5 md:text-sm',
                    isActive
                      ? 'text-sidebar-primary md:bg-sidebar-primary md:text-sidebar-primary-foreground'
                      : 'text-muted-foreground hover:text-sidebar-foreground md:hover:bg-sidebar-accent',
                  )
                }
              >
                <Icon className="size-5 md:size-4" />
                {label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <main className="min-w-0 flex-1 px-4 pt-4 pb-24 md:px-8 md:py-6">
        <Outlet />
      </main>
    </div>
  )
}

export default MainLayout
