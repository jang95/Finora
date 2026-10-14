import { useState, type FormEvent } from 'react'
import FieldError from '@/components/common/FieldError'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/services/api'
import { changePassword } from '@/services/authService'

// 비밀번호 변경 폼 (POST /api/me/password). 제출 흐름은 RegisterPage와 같다.
function PasswordChangeCard() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [newPasswordConfirm, setNewPasswordConfirm] = useState('')
  // 서버가 돌려준 입력칸별 오류. 키는 Backend 이름(CurrentPassword, NewPassword)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  // 입력칸과 관계없는 오류 (서버 연결 실패, 요청이 너무 많음 등)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [submitting, setSubmitting] = useState(false)

  // 파생 값 설명은 RegisterPage 참고
  const isMismatch = newPasswordConfirm !== '' && newPassword !== newPasswordConfirm
  const isSameAsCurrent = newPassword !== '' && newPassword === currentPassword

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    // 화면 검사는 편의용이다. 현재 비밀번호가 맞는지, 비밀번호 규칙은 Backend가 확인한다.
    if (isMismatch || isSameAsCurrent) return
    setFieldErrors({})
    setError('')
    setNotice('')
    setSubmitting(true)
    try {
      await changePassword({ currentPassword, newPassword })
      // 비밀번호는 화면에 남겨두지 않는다. 성공하면 입력칸을 비운다.
      setCurrentPassword('')
      setNewPassword('')
      setNewPasswordConfirm('')
      setNotice('비밀번호를 바꿨습니다. 다른 기기에서는 다시 로그인해야 합니다.')
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) {
        setFieldErrors(err.fieldErrors)
        setError(err.fieldErrors['']?.join(' ') ?? '')
      } else {
        setError(err instanceof ApiError ? err.message : '비밀번호를 바꾸지 못했습니다.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>비밀번호 변경</CardTitle>
        <CardDescription>현재 비밀번호를 확인한 뒤 새 비밀번호로 바꿉니다. 다른 기기의 로그인은 끊깁니다.</CardDescription>
      </CardHeader>
      <CardContent>
        {/* max-w-sm: PC에서 입력칸이 화면 끝까지 길게 늘어나지 않게 폭을 제한한다 */}
        <form onSubmit={handleSubmit} className="flex max-w-sm flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="currentPassword">현재 비밀번호</Label>
            {/* aria-invalid / aria-describedby 설명은 RegisterPage 참고 */}
            <Input
              id="currentPassword"
              type="password"
              autoComplete="current-password"
              required
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              aria-invalid={fieldErrors.CurrentPassword !== undefined}
              aria-describedby="currentPasswordError"
            />
            <FieldError id="currentPasswordError" messages={fieldErrors.CurrentPassword} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="newPassword">새 비밀번호</Label>
            <Input
              id="newPassword"
              type="password"
              autoComplete="new-password"
              required
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              aria-invalid={isSameAsCurrent || fieldErrors.NewPassword !== undefined}
              aria-describedby="newPasswordHint newPasswordError"
            />
            {/* 비밀번호 규칙을 미리 알려준다 (Backend Program.cs의 Identity 설정과 같은 내용) */}
            <p id="newPasswordHint" className="text-xs text-muted-foreground">
              8자 이상, 숫자 1개 이상
            </p>
            {/* 화면 검사 문구가 있으면 그것을, 없으면 서버 문구를 보여준다 */}
            <FieldError
              id="newPasswordError"
              messages={isSameAsCurrent ? ['현재 비밀번호와 다른 비밀번호를 입력하세요.'] : fieldErrors.NewPassword}
            />
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

          {error && (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          )}
          {/* role="status": 알림 메시지를 화면낭독기가 읽어준다 (에러가 아니라서 alert보다 덜 급하게) */}
          {notice && (
            <p role="status" className="text-sm text-muted-foreground">
              {notice}
            </p>
          )}

          <Button type="submit" className="self-start" disabled={submitting || isMismatch || isSameAsCurrent}>
            {submitting ? '변경 중...' : '비밀번호 변경'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

export default PasswordChangeCard
