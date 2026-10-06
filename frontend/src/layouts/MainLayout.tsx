import { NavLink, Outlet } from 'react-router'

const menus = [
  { to: '/', label: '대시보드' },
  { to: '/transactions', label: '가계부' },
  { to: '/assets', label: '자산' },
  { to: '/investments', label: '투자' },
  { to: '/statistics', label: '통계' },
  { to: '/settings', label: '설정' },
]

// 로그인 후 화면의 공통 골격. 메뉴는 고정, <Outlet />에 현재 주소의 페이지가 들어간다.
function MainLayout() {
  return (
    <div>
      <nav>
        <strong>FINORA</strong>
        <ul>
          {menus.map((menu) => (
            <li key={menu.to}>
              <NavLink to={menu.to} end={menu.to === '/'}>
                {menu.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <main>
        <Outlet />
      </main>
    </div>
  )
}

export default MainLayout
