import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

function RegisterPage() {
  // 입력칸마다 state 하나씩 (제어 컴포넌트 설명은 LoginPage 참고)
  const [inviteCode, setInviteCode] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [passwordConfirm, setPasswordConfirm] = useState('')

  // [파생 값] 다른 state로 계산할 수 있는 값은 state로 따로 만들지 않고, 그릴 때마다 계산한다.
  // state로 두면 password가 바뀔 때마다 이 값도 맞춰서 바꿔야 해서 어긋나기 쉽다.
  // 확인 칸이 비어 있을 때는 아직 입력 중이므로 오류로 보지 않는다.
  const isPasswordMismatch = passwordConfirm !== '' && password !== passwordConfirm

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    // 화면 검사는 편의용이다. 최종 검사(초대코드 유효성, 비밀번호 규칙)는 Backend가 한다.
    if (isPasswordMismatch) return
    // TODO(Phase 4): authService.register({ inviteCode, email, password }) 호출 후 로그인 화면으로 이동
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-2">
        <Label htmlFor="inviteCode">초대코드</Label>
        {/* 초대코드는 자동입력 대상이 아니므로 autoComplete="off" */}
        <Input
          id="inviteCode"
          autoComplete="off"
          required
          value={inviteCode}
          onChange={(event) => setInviteCode(event.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="email">이메일</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="name@example.com"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="password">비밀번호</Label>
        {/* new-password: 브라우저가 기존 비밀번호 대신 새 비밀번호 생성/저장을 제안한다 */}
        <Input
          id="password"
          type="password"
          autoComplete="new-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="passwordConfirm">비밀번호 확인</Label>
        {/* aria-invalid: 화면낭독기에 "잘못된 값"이라고 알린다. shadcn Input은 이 값이 true면 빨간 테두리로 바뀐다.
            aria-describedby: 아래 오류 문구의 id와 연결해서, 이 칸에 포커스가 가면 오류 문구도 읽어준다. */}
        <Input
          id="passwordConfirm"
          type="password"
          autoComplete="new-password"
          required
          value={passwordConfirm}
          onChange={(event) => setPasswordConfirm(event.target.value)}
          aria-invalid={isPasswordMismatch}
          aria-describedby="passwordConfirmError"
        />
        {isPasswordMismatch && (
          <p id="passwordConfirmError" className="text-sm text-destructive">
            비밀번호가 일치하지 않습니다.
          </p>
        )}
      </div>

      <Button type="submit" className="mt-2 w-full" disabled={isPasswordMismatch}>
        가입하기
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        이미 계정이 있으신가요?{' '}
        <Link to="/login" className="font-medium text-foreground underline-offset-4 hover:underline">
          로그인
        </Link>
      </p>
    </form>
  )
}

export default RegisterPage
