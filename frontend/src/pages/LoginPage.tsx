import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/services/api'
import { login } from '@/services/authService'

function LoginPage() {
  // useNavigate: 코드에서 다른 주소로 이동할 때 쓰는 함수를 돌려준다. (Link는 사용자가 누르는 이동)
  const navigate = useNavigate()
  // location.state: 이전 화면이 navigate(..., { state })로 넘겨준 값. 가입 직후면 { registered: true }
  const location = useLocation()
  const justRegistered = (location.state as { registered?: boolean } | null)?.registered === true

  // [제어 컴포넌트] 입력칸의 값을 React state가 쥐고 있는 방식.
  //   value={email}            → 입력칸에 보이는 값은 항상 state 값
  //   onChange={... setEmail}  → 키를 누를 때마다 state를 바꾸고, 바뀐 state가 다시 입력칸에 표시된다
  // 이렇게 하면 제출할 때 state를 바로 쓸 수 있고, 입력 중 검사·초기화도 state만 바꾸면 된다.
  // (value만 주고 onChange를 빼면 입력해도 글자가 바뀌지 않는다)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  // 서버가 돌려준 오류 문구 (비밀번호 틀림, 계정 잠김 등)
  const [error, setError] = useState('')
  // 요청 중에는 버튼을 막아 두 번 제출되지 않게 한다.
  const [submitting, setSubmitting] = useState(false)

  // HTML form은 제출하면 페이지를 새로고침한다. React에서는 preventDefault()로 이를 막고 직접 처리한다.
  // async 함수: 안에서 await로 서버 응답을 기다릴 수 있다.
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await login({ email, password, rememberMe })
      // replace: 뒤로 가기를 눌러도 로그인 화면으로 돌아오지 않게, 기록을 덮어쓴다.
      navigate('/', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : '로그인하지 못했습니다.')
      // 실패하면 비밀번호만 비운다. (이메일은 다시 입력하지 않아도 되게 남긴다)
      setPassword('')
    } finally {
      // finally: 성공이든 실패든 마지막에 항상 실행된다.
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {justRegistered && (
        <p role="status" className="text-sm text-muted-foreground">
          가입이 완료됐습니다. 로그인하세요.
        </p>
      )}
      {/* Label의 htmlFor와 Input의 id가 같아야 글자를 눌렀을 때 입력칸에 포커스가 간다.
          type="email"(형식 검사), required(빈칸 경고), autoComplete(저장된 계정 자동입력)는 브라우저가 처리한다. */}
      <div className="flex flex-col gap-2">
        <Label htmlFor="email">이메일</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="name@example.com"
          required
          value={email}
          // event.target: 이벤트가 일어난 입력칸. .value가 현재 입력된 글자다.
          onChange={(event) => setEmail(event.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="password">비밀번호</Label>
        <Input
          id="password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
      </div>
      <div className="flex items-center gap-2">
        {/* shadcn Checkbox는 onChange 대신 onCheckedChange로 체크 상태(true/false)를 알려준다 */}
        <Checkbox
          id="rememberMe"
          checked={rememberMe}
          onCheckedChange={(checked) => setRememberMe(checked === true)}
        />
        <Label htmlFor="rememberMe" className="font-normal">
          로그인 유지
        </Label>
      </div>

      {/* role="alert": 오류 문구가 나타나면 화면낭독기가 바로 읽어준다 */}
      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}

      <Button type="submit" className="mt-2 w-full" disabled={submitting}>
        {submitting ? '로그인 중...' : '로그인'}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        계정이 없으신가요?{' '}
        {/* <a href>는 페이지 전체를 다시 불러오고, Link는 React Router가 화면만 바꾼다. 앱 내부 이동은 Link를 쓴다. */}
        <Link to="/register" className="font-medium text-foreground underline-offset-4 hover:underline">
          회원가입
        </Link>
      </p>
    </form>
  )
}

export default LoginPage
