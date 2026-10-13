import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router'
import './index.css'
import { queryClient } from './lib/queryClient'
import { router } from './router'

// QueryClientProvider: 안쪽의 모든 화면이 같은 queryClient(데이터 캐시)를 쓰게 한다. (lib/queryClient.ts 설명 참고)
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
)
