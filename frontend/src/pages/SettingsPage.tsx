import { useQuery, useQueryClient } from '@tanstack/react-query'
import AccountCard from '@/components/settings/AccountCard'
import CategorySection from '@/components/settings/CategorySection'
import PasswordChangeCard from '@/components/settings/PasswordChangeCard'
import ServerStatusCard from '@/components/settings/ServerStatusCard'
import {
  addCategory,
  categoriesQueryKey,
  deleteCategory,
  getCategories,
  renameCategory,
} from '@/services/categoryService'
import { transactionsQueryKey } from '@/services/transactionService'
import type { CategoryType } from '@/types/category'

function SettingsPage() {
  // [useQuery] 데이터 불러오기를 TanStack Query에 맡긴다. (설명은 lib/queryClient.ts)
  //   queryKey: 캐시 이름표. 같은 이름표를 쓰는 화면끼리 데이터를 함께 쓴다 (가계부 입력 창의 카테고리 목록 등)
  //   queryFn: 실제로 불러오는 함수 (service)
  //   돌려주는 값: data(데이터, 오기 전에는 undefined), isPending(처음 불러오는 중), error(실패 시 오류)
  // useEffect + ignore 플래그 + reloadKey로 하던 일이 이 안에 다 들어 있다.
  const { data: categories, isPending, error } = useQuery({
    queryKey: categoriesQueryKey,
    queryFn: getCategories,
  })
  // useQueryClient: main.tsx에서 넘겨준 queryClient(캐시 저장소)를 꺼낸다.
  const queryClient = useQueryClient()

  // 저장한 뒤 "카테고리 데이터가 낡았다"고 알린다 → useQuery가 자동으로 다시 불러와서 화면이 바뀐다.
  // await: 다시 불러오기가 끝난 뒤에 입력칸을 비우도록 기다린다.
  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
    // 가계부에 표시되는 카테고리 이름도 바뀌었을 수 있으므로 거래 데이터도 다시 불러오게 한다.
    await queryClient.invalidateQueries({ queryKey: transactionsQueryKey })
  }

  // 수입/지출 섹션이 같은 처리를 쓰도록 종류만 받아서 props 묶음을 만든다.
  // 실패하면 service가 ApiError를 던지고, CategorySection이 받아서 문구를 보여준다.
  function sectionProps(type: CategoryType) {
    return {
      categories: (categories ?? []).filter((c) => c.type === type),
      // parentId가 있으면 하위 분류. 종류(type)는 Backend가 상위 카테고리를 따라 정한다
      onAdd: async (name: string, parentId?: number) => {
        await addCategory({ name, type, parentId })
        await refresh()
      },
      onRename: async (id: number, name: string) => {
        await renameCategory(id, name)
        await refresh()
      },
      onDelete: async (id: number) => {
        await deleteCategory(id)
        await refresh()
      },
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">설정</h1>

      {isPending ? (
        <p className="text-muted-foreground">불러오는 중...</p>
      ) : error ? (
        <p role="alert" className="text-destructive">
          {error.message}
        </p>
      ) : (
        // PC에서는 수입/지출을 나란히, 모바일에서는 위아래로
        <div className="grid gap-6 md:grid-cols-2">
          <CategorySection title="지출 카테고리" {...sectionProps('Expense')} />
          <CategorySection title="수입 카테고리" {...sectionProps('Income')} />
        </div>
      )}

      <AccountCard />

      <PasswordChangeCard />

      <ServerStatusCard />
    </div>
  )
}

export default SettingsPage
