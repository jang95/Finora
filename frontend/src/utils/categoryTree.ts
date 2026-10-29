import { MAX_CATEGORY_DEPTH, type Category } from '@/types/category'

// 카테고리 계층(대분류 › 중분류 › 소분류) 계산. 설정·거래 입력·통계 화면이 같이 쓴다. (결정 D23)

// 트리 순서로 펼친 한 줄. depth: 대분류 1, 중분류 2, 소분류 3
export interface CategoryRow {
  category: Category
  depth: number
}

// 평평한 목록을 "부모 바로 아래에 자식"이 오는 순서로 펼친다. 들여쓰기만 하면 트리처럼 보인다.
//   교통(1) → 대중교통(2) → 버스(3) → 지하철(3) → 택시(2) → 식비(1) ...
// 같은 부모 안의 순서는 Backend가 보낸 순서(SortOrder)를 그대로 따른다.
export function flattenCategoryTree(categories: Category[]): CategoryRow[] {
  // [Map] 키 → 값 사전. C#의 Dictionary<int?, List<Category>>와 같다.
  //   부모 id(대분류는 null)별로 자식 목록을 미리 묶어 두면 자식을 찾을 때 매번 전체를 뒤지지 않아도 된다.
  const ids = new Set(categories.map((c) => c.id))
  const childrenOf = new Map<number | null, Category[]>()
  for (const category of categories) {
    // 부모가 목록에 없으면(정상이라면 없는 경우) 대분류로 보여서 화면에서 사라지지 않게 한다
    const parentId = category.parentId !== null && ids.has(category.parentId) ? category.parentId : null
    // [?? 연산자] 왼쪽이 null/undefined면 오른쪽 값. C#의 ??와 같다
    const siblings = childrenOf.get(parentId) ?? []
    siblings.push(category)
    childrenOf.set(parentId, siblings)
  }

  const rows: CategoryRow[] = []
  // [재귀] 함수가 자기 자신을 다시 부른다. 자식에게 같은 일을 depth + 1로 시키면 몇 단계든 펼쳐진다.
  function visit(parentId: number | null, depth: number) {
    for (const category of childrenOf.get(parentId) ?? []) {
      rows.push({ category, depth })
      visit(category.id, depth + 1)
    }
  }
  visit(null, 1)
  return rows
}

// 카테고리의 전체 경로 이름. 예: ['교통', '대중교통', '버스']. 못 찾으면 빈 배열
// 부모를 따라 위로 올라가면서 앞에 붙인다. 최대 3단계지만, 잘못된 데이터로 끝없이 돌지 않게 횟수를 제한한다.
export function getCategoryPath(categories: Category[], id: number): string[] {
  const byId = new Map(categories.map((c) => [c.id, c]))
  const path: string[] = []
  let current = byId.get(id)
  while (current !== undefined && path.length < MAX_CATEGORY_DEPTH) {
    // unshift: 배열 맨 앞에 넣는다 (push는 맨 뒤). 위로 올라가며 넣으니 대분류가 맨 앞에 온다
    path.unshift(current.name)
    current = current.parentId === null ? undefined : byId.get(current.parentId)
  }
  return path
}
