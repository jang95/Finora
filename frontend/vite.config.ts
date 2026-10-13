import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // '@/components/...' → 'src/components/...'
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    // 개발 중 '/api'로 시작하는 요청을 Backend(dotnet run, :5153)로 넘긴다.
    // 화면은 fetch('/api/health')처럼 같은 주소로만 요청하므로 CORS 설정이 필요 없고,
    // 운영에서 Backend가 React 파일을 함께 제공할 때와 코드가 똑같다. (계획서 2장)
    proxy: {
      '/api': 'http://localhost:5153',
    },
  },
})
