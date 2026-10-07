import type { FormEvent } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

function RegisterPage() {
  // 실제 가입(초대코드 확인 포함)은 Phase 4에서 services/를 통해 연결한다.
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-2">
        <Label htmlFor="inviteCode">초대코드</Label>
        {/* 초대코드는 자동입력 대상이 아니므로 autoComplete="off" */}
        <Input id="inviteCode" autoComplete="off" required />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="email">이메일</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="name@example.com"
          required
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="password">비밀번호</Label>
        {/* new-password: 브라우저가 기존 비밀번호 대신 새 비밀번호 생성/저장을 제안한다 */}
        <Input id="password" type="password" autoComplete="new-password" required />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="passwordConfirm">비밀번호 확인</Label>
        <Input id="passwordConfirm" type="password" autoComplete="new-password" required />
      </div>

      <Button type="submit" className="mt-2 w-full">
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
