import { NavLink, Outlet } from 'react-router'
import styles from './MainLayout.module.css'

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
    <div className={styles.layout}>
      <nav className={styles.sidebar}>
        <strong className={styles.logo}>FINORA</strong>
        <ul className={styles.menu}>
          {menus.map((menu) => (
            <li key={menu.to}>
              <NavLink
                to={menu.to}
                end={menu.to === '/'}
                className={({ isActive }) =>
                  isActive ? `${styles.link} ${styles.active}` : styles.link
                }
              >
                {menu.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <main className={styles.content}>
        <Outlet />
      </main>
    </div>
  )
}

export default MainLayout
