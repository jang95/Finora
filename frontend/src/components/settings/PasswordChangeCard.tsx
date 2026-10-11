import { useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

// 비밀번호 변경 폼. 화면만 있고 실제 변경은 로그인 기능(Phase 4)이 생긴 뒤 연결한다.
function PasswordChangeCard() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [newPasswordConfirm, setNewPasswordConfirm] = useState('')
  const [notice, setNotice] = useState('')

  // 파생 값 설명은 RegisterPage 참고
  const isMismatch = newPasswordConfirm !== '' && newPassword !== newPasswordConfirm
  const isSameAsCurrent = newPassword !== '' && newPassword === currentPassword

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    // 화면 검사는 편의용이다. 현재 비밀번호가 맞는지, 비밀번호 규칙은 Backend가 확인한다.
    if (isMismatch || isSameAsCurrent) return
    // TODO(Phase 4): authService.changePassword({ currentPassword, newPassword }) 호출
    // 비밀번호는 화면에 남겨두지 않는다. 처리 후 입력칸을 비운다.
    setCurrentPassword('')
    setNewPassword('')
    setNewPasswordConfirm('')
    setNotice('비밀번호 변경은 로그인 기능이 연결된 뒤 동작합니다.')
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>비밀번호 변경</CardTitle>
        <CardDescription>현재 비밀번호를 확인한 뒤 새 비밀번호로 바꿉니다.</CardDescription>
      </CardHeader>
      <CardContent>
        {/* max-w-sm: PC에서 입력칸이 화면 끝까지 길게 늘어나지 않게 폭을 제한한다 */}
        <form onSubmit={handleSubmit} className="flex max-w-sm flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="currentPassword">현재 비밀번호</Label>
            <Input
              id="currentPassword"
              type="password"
              autoComplete="current-password"
              required
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="newPassword">새 비밀번호</Label>
            {/* aria-invalid / aria-describedby 설명은 RegisterPage 참고 */}
            <Input
              id="newPassword"
              type="password"
              autoComplete="new-password"
              required
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              aria-invalid={isSameAsCurrent}
              aria-describedby="newPasswordError"
            />
            {isSameAsCurrent && (
              <p id="newPasswordError" className="text-sm text-destructive">
                현재 비밀번호와 다른 비밀번호를 입력하세요.
              </p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="newPasswordConfirm">새 비밀번호 확인</Label>
            <Input
              id="newPasswordConfirm"
              type="password"
              autoComplete="new-password"
              required
              value={newPasswordConfirm}
              onChange={(event) => setNewPasswordConfirm(event.target.value)}
              aria-invalid={isMismatch}
              aria-describedby="newPasswordConfirmError"
            />
            {isMismatch && (
              <p id="newPasswordConfirmError" className="text-sm text-destructive">
                새 비밀번호가 일치하지 않습니다.
              </p>
            )}
          </div>

          {/* role="status": 알림 메시지를 화면낭독기가 읽어준다 (에러가 아니라서 alert보다 덜 급하게) */}
          {notice && (
            <p role="status" className="text-sm text-muted-foreground">
              {notice}
            </p>
          )}

          <Button type="submit" className="self-start" disabled={isMismatch || isSameAsCurrent}>
            비밀번호 변경
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

export default PasswordChangeCard
