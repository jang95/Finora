import { useRouteError } from 'react-router'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/services/api'

// loader나 화면에서 처리하지 못한 오류가 났을 때 보여주는 화면 (router.tsx의 ErrorBoundary)
// 예: Backend가 꺼져 있어서 로그인 상태를 확인하지 못한 경우
function ErrorPage() {
  // useRouteError: 이 화면을 띄운 원인 오류를 꺼낸다.
  const error = useRouteError()
  // 오류 내용은 우리가 만든 ApiError일 때만 보여준다. 그 밖의 오류 내용은 사용자에게 의미가 없다.
  const message = error instanceof ApiError ? error.message : '예상하지 못한 오류가 발생했습니다.'

  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-4 p-4 text-center">
      <p className="text-lg font-medium">{message}</p>
      {/* 새로고침으로 loader를 처음부터 다시 실행한다 */}
      <Button variant="outline" onClick={() => window.location.reload()}>
        다시 시도
      </Button>
    </main>
  )
}

export default ErrorPage
