// 서버 오류 문구 한 줄. 입력칸의 aria-describedby와 id로 연결한다. (aria 설명은 RegisterPage 비밀번호 확인 칸 참고)
// 회원가입·비밀번호 변경 화면이 같이 쓴다.
function FieldError({ id, messages }: { id: string; messages?: string[] }) {
  if (!messages?.length) return null
  return (
    <p id={id} className="text-sm text-destructive">
      {messages.join(' ')}
    </p>
  )
}

export default FieldError
