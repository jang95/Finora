import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, RotateCcw, SlidersHorizontal } from 'lucide-react'
import PeriodPicker from '@/components/common/PeriodPicker'
import StatisticsDashboard from '@/components/statistics/StatisticsDashboard'
import { DEFAULT_LAYOUT } from '@/components/statistics/widgets'
import { Button } from '@/components/ui/button'
import { getStatisticsLayout, saveStatisticsLayout, statisticsLayoutQueryKey } from '@/services/dashboardLayoutService'
import type { WidgetLayoutItem } from '@/types/dashboardLayout'
import { mergeLayout } from '@/utils/dashboardLayout'
import { getPeriod } from '@/utils/period'
import { getToday } from '@/utils/yearMonth'

// 배치를 바꾼 뒤 이만큼 조용하면 저장한다 (ms). 드래그를 연달아 해도 마지막 한 번만 보낸다
const SAVE_DELAY = 800

// 통계 화면 = 기간 선택 + 위젯 대시보드 (결정 D26).
// 화면은 기간과 배치만 들고 있고, 데이터는 위젯이 각자 불러온다 (이유는 PeriodSummaryWidget 참고).
// 배치는 서버에 사용자별로 저장한다 (결정 D27): 들어올 때 불러오고, 바꾸면 잠시 뒤 저장한다.
function StatisticsPage() {
  // 고른 기간. 처음엔 이번 달. 대시보드 화면과 공유하지 않는다 (화면마다 따로)
  const [period, setPeriod] = useState(() => getPeriod('month', getToday()))
  // 편집 모드: 켜면 위젯 위에 손잡이·크기·숨기기 도구가 나오고 드래그할 수 있다 (결정 D26)
  const [isEditing, setIsEditing] = useState(false)

  // 저장된 배치 불러오기. 거래와 상관없는 데이터라 이름표가 따로다 (dashboardLayoutService 참고)
  const layoutQuery = useQuery({
    queryKey: statisticsLayoutQueryKey,
    queryFn: getStatisticsLayout,
  })

  // [서버 값 vs 편집 중인 값] 서버에서 온 배치는 TanStack Query 캐시에, 사용자가 바꾸는 중인 배치는 draft에 둔다.
  //   draft가 null이면 아직 안 바꾼 것 → 서버 배치(기본 배치와 합친 것)를 보여준다.
  //   바꾸는 순간 draft가 생기고, 그 뒤로는 draft를 보여준다 (저장이 끝나기를 기다리지 않아서 화면이 바로 움직인다)
  const [draft, setDraft] = useState<WidgetLayoutItem[] | null>(null)
  const layout = draft ?? mergeLayout(layoutQuery.data?.items ?? [], DEFAULT_LAYOUT)

  // [useMutation] 데이터를 "바꾸는" 요청(저장·삭제)용 훅. useQuery가 "읽기"라면 useMutation은 "쓰기".
  //   mutate(값)을 부르면 mutationFn이 실행되고, isPending / error 상태를 대신 관리해 준다.
  //   onSuccess: 서버가 돌려준(저장된) 배치를 캐시에 바로 넣는다 → 다음에 이 화면에 들어오면 그 배치로 시작한다.
  //   invalidate(다시 불러오기) 대신 setQueryData(직접 넣기)를 쓰는 이유: 응답에 이미 최신 값이 있어서 한 번 더 불러올 필요가 없다
  const queryClient = useQueryClient()
  const { mutate: saveLayout, isPending: isSaving, error: saveError } = useMutation({
    mutationFn: saveStatisticsLayout,
    onSuccess: (saved) => queryClient.setQueryData(statisticsLayoutQueryKey, saved),
  })

  // 아직 저장하지 않은 배치. 화면을 떠날 때 타이머를 기다리지 않고 바로 보내려고 ref에 들고 있는다
  // [useRef] 값을 바꿔도 화면을 다시 그리지 않는 "메모장". 다시 그릴 필요 없는 값(타이머, 보류 중인 값)에 쓴다
  const pendingRef = useRef<WidgetLayoutItem[] | null>(null)

  function changeLayout(next: WidgetLayoutItem[]) {
    setDraft(next)
    pendingRef.current = next
  }

  // [debounce] draft가 바뀔 때마다 타이머를 새로 건다. SAVE_DELAY 안에 또 바뀌면 cleanup이 이전 타이머를 지우므로,
  //   연달아 바꾸는 동안에는 저장하지 않고 손을 뗀 뒤 한 번만 저장한다.
  //   useEffect의 return(cleanup)은 "다음 실행 직전"과 "화면을 떠날 때" 불린다
  useEffect(() => {
    if (draft === null) return
    const timer = setTimeout(() => {
      pendingRef.current = null
      saveLayout(draft)
    }, SAVE_DELAY)
    return () => clearTimeout(timer)
  }, [draft, saveLayout])

  // 화면을 떠날 때(다른 메뉴로 이동) 타이머가 남아 있으면 기다리지 않고 바로 저장한다.
  // 의존성 배열이 [saveLayout]뿐이라 이 effect는 처음 한 번만 실행되고, cleanup은 떠날 때 한 번만 불린다
  useEffect(() => {
    return () => {
      if (pendingRef.current !== null) saveLayout(pendingRef.current)
    }
  }, [saveLayout])

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">통계</h1>
        <div className="flex items-center gap-2">
          {/* 저장 상태. 평소엔 아무것도 없고, 저장 중이면 잠깐 문구, 실패하면 오류 문구 */}
          {isSaving && <span className="text-xs text-muted-foreground">저장 중...</span>}
          {saveError && (
            <span role="alert" className="text-xs text-destructive">
              배치 저장 실패: {saveError.message}
            </span>
          )}
          {isEditing && (
            <Button type="button" variant="ghost" size="sm" onClick={() => changeLayout(DEFAULT_LAYOUT)}>
              <RotateCcw />
              기본 배치
            </Button>
          )}
          {/* 같은 버튼이 "배치 편집" ↔ "완료"로 바뀐다 */}
          <Button type="button" variant={isEditing ? 'default' : 'outline'} size="sm" onClick={() => setIsEditing((v) => !v)}>
            {isEditing ? <Check /> : <SlidersHorizontal />}
            {isEditing ? '완료' : '배치 편집'}
          </Button>
        </div>
      </div>

      <PeriodPicker value={period} onChange={setPeriod} />

      {/* 배치를 불러오기 전에 기본 배치를 먼저 그리면 잠시 뒤 저장된 배치로 "튀어" 보여서, 올 때까지 기다린다.
          불러오기에 실패하면 안내만 하고 기본 배치로 보여준다 (통계 자체는 볼 수 있게) */}
      {layoutQuery.error && (
        <p role="alert" className="text-sm text-destructive">
          저장된 배치를 불러오지 못해 기본 배치로 보여줍니다: {layoutQuery.error.message}
        </p>
      )}
      {layoutQuery.isPending ? (
        <p className="text-muted-foreground">불러오는 중...</p>
      ) : (
        <StatisticsDashboard layout={layout} period={period} isEditing={isEditing} onLayoutChange={changeLayout} />
      )}
    </div>
  )
}

export default StatisticsPage
