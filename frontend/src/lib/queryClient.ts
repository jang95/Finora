import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/services/api'

// [TanStack Query] 서버에서 받아 온 데이터를 보관(캐시)하고, "불러오는 중 / 오류 / 데이터" 상태를 대신 관리해 주는 라이브러리.
//   지금까지 화면마다 useEffect + useState + reloadKey로 하던 일을 useQuery 한 줄로 바꾼다.
//   데이터를 바꾼 뒤(useMutation)에는 "이 데이터가 낡았다(invalidate)"고 알리기만 하면, 그 데이터를 쓰는 모든 화면이 다시 불러온다.
// QueryClient = 그 캐시 저장소. 앱 전체에 하나만 만든다. (main.tsx의 QueryClientProvider로 전달)
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 실패하면 한 번 더 시도한다. 단, 4xx(401 로그인 필요, 404 없음 등)는 다시 해도 같으므로 바로 실패로 처리한다.
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false
        return failureCount < 1
      },
    },
  },
})
