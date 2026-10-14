import { useEffect, useState } from 'react'
import ComingSoon from '@/components/common/ComingSoon'
import MoneyText from '@/components/common/MoneyText'
import { FEATURES } from '@/config/features'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { getAssetSummary } from '@/services/assetService'
import type { AccountType, AssetSummary } from '@/types/asset'

// 계좌 종류 → 화면 표시 이름.
// Record<키 타입, 값 타입>: 키가 정해진 객체 타입. AccountType의 5개를 모두 적지 않으면 에러가 나서 빠뜨릴 수 없다.
const ACCOUNT_TYPE_LABELS: Record<AccountType, string> = {
  Cash: '현금',
  Bank: '은행',
  Deposit: '예금',
  Savings: '적금',
  Other: '기타',
}

// 계좌 기능이 꺼져 있으면 "준비 중"을 보여준다. 아래 계좌 화면 코드는 Phase 8에서 다시 쓰려고 남겨 둔다. (결정 D16)
// 스위치 검사를 AccountAssets 안에서 하지 않고 바깥 컴포넌트로 나눈 이유:
//   React Hook(useState 등)은 조건문보다 먼저, 항상 같은 순서로 호출해야 한다. return을 Hook보다 앞에 둘 수 없다.
function AssetsPage() {
  if (!FEATURES.accounts) {
    return (
      <div className="flex flex-col gap-6">
        <h1 className="text-2xl font-bold">자산</h1>
        <ComingSoon
          title="자산(계좌) 관리는 준비 중입니다."
          description="가계부 기능을 먼저 제공하고, 계좌·이체 기능은 이후에 추가될 예정입니다."
        />
      </div>
    )
  }
  return <AccountAssets />
}

// 계좌 목록 + 총자산 (Phase 2 임시 데이터 화면. Phase 8에서 실제 API로 연결)
function AccountAssets() {
  const [summary, setSummary] = useState<AssetSummary | null>(null)

  // 데이터 불러오기 방식은 DashboardPage 설명 참고
  useEffect(() => {
    let ignore = false

    getAssetSummary().then((data) => {
      if (!ignore) setSummary(data)
    })

    return () => {
      ignore = true
    }
  }, [])

  if (summary === null) {
    return <p className="text-muted-foreground">불러오는 중...</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">자산</h1>

      <Card>
        <CardHeader>
          <CardDescription>총자산</CardDescription>
          <CardTitle className="text-2xl font-bold">
            <MoneyText amount={summary.totalBalance} />
          </CardTitle>
        </CardHeader>
      </Card>

      {summary.accounts.length === 0 ? (
        <p className="text-muted-foreground">등록된 계좌가 없습니다.</p>
      ) : (
        <Card className="gap-0 py-0">
          <ul className="divide-y">
            {summary.accounts.map((account) => (
              <li key={account.id} className="flex items-center justify-between gap-4 px-4 py-3">
                <div className="min-w-0">
                  <p className="truncate font-medium">{account.name}</p>
                  <p className="text-sm text-muted-foreground">{ACCOUNT_TYPE_LABELS[account.type]}</p>
                </div>
                <MoneyText amount={account.balance} className="shrink-0 font-semibold" />
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  )
}

export default AssetsPage
