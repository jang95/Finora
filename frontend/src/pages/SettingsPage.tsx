import { useEffect, useState } from 'react'
import AccountCard from '@/components/settings/AccountCard'
import CategorySection from '@/components/settings/CategorySection'
import PasswordChangeCard from '@/components/settings/PasswordChangeCard'
import ServerStatusCard from '@/components/settings/ServerStatusCard'
import { addCategory, deleteCategory, getCategories, renameCategory } from '@/services/categoryService'
import type { Category, CategoryType } from '@/types/category'

function SettingsPage() {
  const [categories, setCategories] = useState<Category[] | null>(null)
  // 다시 불러오기 방식은 TransactionsPage의 reloadKey 설명 참고
  const [reloadKey, setReloadKey] = useState(0)

  // 데이터 불러오기 방식은 DashboardPage 설명 참고
  useEffect(() => {
    let ignore = false

    getCategories().then((data) => {
      // [...data]: 배열을 복사한다. mock 배열을 그대로 state에 넣으면 service가 그 배열을 바꿔도
      //   React는 "같은 배열"이라 바뀐 줄 모르고 다시 그리지 않는다. 새 배열이어야 변경으로 인식한다.
      if (!ignore) setCategories([...data])
    })

    return () => {
      ignore = true
    }
  }, [reloadKey])

  function reload() {
    setReloadKey((prev) => prev + 1)
  }

  // 수입/지출 섹션이 같은 처리를 쓰도록 종류만 받아서 props 묶음을 만든다
  function sectionProps(type: CategoryType) {
    return {
      categories: (categories ?? []).filter((c) => c.type === type),
      onAdd: async (name: string) => {
        await addCategory({ name, type })
        reload()
      },
      onRename: async (id: number, name: string) => {
        await renameCategory(id, name)
        reload()
      },
      onDelete: async (id: number) => {
        await deleteCategory(id)
        reload()
      },
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">설정</h1>

      {categories === null ? (
        <p className="text-muted-foreground">불러오는 중...</p>
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
