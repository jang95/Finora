// 모든 service가 함께 쓰는 API 호출 도우미.
// Backend의 오류 응답(ProblemDetails)을 ApiError로 바꿔서, 화면은 try/catch로 문구만 꺼내 쓰면 되게 한다.

// Backend 오류 응답 모양 (Program.cs의 AddProblemDetails, ValidationProblem)
type ProblemDetails = {
  status?: number
  title?: string
  detail?: string
  // 입력칸별 오류. 예: { InviteCode: ['유효하지 않은 초대코드입니다.'] }
  errors?: Record<string, string[]>
}

// API가 실패했을 때 던지는 오류. Error를 상속(extends)해서 일반 오류처럼 throw/catch할 수 있다.
export class ApiError extends Error {
  readonly status: number
  // 입력칸 이름 → 오류 문구들. 키는 Backend DTO 속성 이름(InviteCode, Email, Password...)이다.
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

// fetch를 감싼다. body가 있으면 JSON으로 보낸다. 실패 응답이면 ApiError를 던진다.
// <T>: 성공 응답 본문의 타입. 호출하는 쪽에서 apiRequest<Me>('/api/me')처럼 정한다.
// Cookie는 같은 주소(same-origin)라 브라우저가 자동으로 보낸다. (계획서 2장)
export async function apiRequest<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, {
      method: options.method ?? 'GET',
      headers: options.body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    // 네트워크 자체가 실패하면 fetch가 예외를 던진다. (healthService 참고)
    throw new ApiError(0, '서버에 연결할 수 없습니다. 잠시 후 다시 시도하세요.')
  }

  if (response.ok) {
    // 204 No Content는 본문이 없다. response.json()을 부르면 오류가 나므로 건너뛴다.
    if (response.status === 204) return undefined as T
    return (await response.json()) as T
  }

  throw await toApiError(response)
}

async function toApiError(response: Response): Promise<ApiError> {
  if (response.status === 429) {
    return new ApiError(429, '요청이 너무 많습니다. 1분 뒤에 다시 시도하세요.')
  }

  // 본문이 ProblemDetails가 아닐 수도 있다(예: proxy 오류). 읽기에 실패하면 빈 값으로 둔다.
  const problem: ProblemDetails = await response.json().catch(() => ({}))
  // Backend가 꺼져 있으면 Vite proxy(운영에서는 웹 서버)가 ProblemDetails가 아닌 5xx를 돌려준다.
  // Backend가 직접 보낸 오류에는 항상 status가 들어 있으므로, 없으면 서버에 닿지 못한 것으로 본다.
  if (response.status >= 500 && problem.status === undefined) {
    return new ApiError(response.status, '서버에 연결할 수 없습니다. 잠시 후 다시 시도하세요.')
  }
  const message = problem.detail ?? '요청을 처리하지 못했습니다. 잠시 후 다시 시도하세요.'
  return new ApiError(response.status, message, problem.errors)
}
