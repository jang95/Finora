import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

function LoginPage() {
  // [제어 컴포넌트] 입력칸의 값을 React state가 쥐고 있는 방식.
  //   value={email}            → 입력칸에 보이는 값은 항상 state 값
  //   onChange={... setEmail}  → 키를 누를 때마다 state를 바꾸고, 바뀐 state가 다시 입력칸에 표시된다
  // 이렇게 하면 제출할 때 state를 바로 쓸 수 있고, 입력 중 검사·초기화도 state만 바꾸면 된다.
  // (value만 주고 onChange를 빼면 입력해도 글자가 바뀌지 않는다)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  // HTML form은 제출하면 페이지를 새로고침한다. React에서는 preventDefault()로 이를 막고 직접 처리한다.
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    // TODO(Phase 4): authService.login({ email, password }) 호출 후 대시보드로 이동
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
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

      <Button type="submit" className="mt-2 w-full">
        로그인
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
