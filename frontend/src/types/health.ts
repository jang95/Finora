// GET /api/health 결과를 화면용으로 정리한 상태.
//   ok          : 서버와 DB 모두 정상
//   dbDown      : 서버는 응답했지만 DB에 연결되지 않음 (503)
//   serverDown  : 서버에 연결할 수 없음 (Backend가 꺼져 있거나 네트워크 오류)
export type ServerStatus = 'ok' | 'dbDown' | 'serverDown'
