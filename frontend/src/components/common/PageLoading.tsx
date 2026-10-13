// 첫 화면에서 router loader(로그인 확인 등)가 끝나기 전에 잠깐 보이는 내용 (router.tsx의 HydrateFallback)
function PageLoading() {
  return <p className="p-8 text-center text-muted-foreground">불러오는 중...</p>
}

export default PageLoading
