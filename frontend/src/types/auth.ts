// 현재 로그인한 사용자 (GET /api/me 응답)
export interface Me {
  email: string
}

// 로그인 입력값 (POST /api/auth/login)
export interface LoginInput {
  email: string
  password: string
  // 로그인 유지. true면 브라우저를 닫아도 14일 동안 로그인이 유지된다.
  rememberMe: boolean
}

// 회원가입 입력값 (POST /api/auth/register)
export interface RegisterInput {
  inviteCode: string
  email: string
  password: string
}
