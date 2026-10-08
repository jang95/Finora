import { useEffect, useState } from 'react'
import MoneyText from '@/components/common/MoneyText'
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

function AssetsPage() {
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
