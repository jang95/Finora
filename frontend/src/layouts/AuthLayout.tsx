import { Outlet } from 'react-router'
import styles from './AuthLayout.module.css'

// 로그인·회원가입 화면의 골격. 메뉴 없이 가운데 카드 안에 폼만 보여준다.
function AuthLayout() {
  return (
    <main className={styles.layout}>
      <div className={styles.card}>
        <strong className={styles.logo}>FINORA</strong>
        <Outlet />
      </div>
    </main>
  )
}

export default AuthLayout
