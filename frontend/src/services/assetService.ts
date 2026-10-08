import { accountsMock } from '@/mocks/accounts'
import type { Account, AssetSummary } from '@/types/asset'

// Backend가 생기면 GET /api/accounts 호출로 바꾼다.
export async function getAccounts(): Promise<Account[]> {
  return accountsMock
}

// 자산 화면용 (계좌 목록 + 합계). Backend가 생기면 GET /api/assets/summary 같은 호출로 바꾼다.
// 지금은 Backend 대신 여기서 합계를 낸다. 화면(pages)에서는 금액을 더하지 않는다 (결정 D4)
export async function getAssetSummary(): Promise<AssetSummary> {
  // reduce: 배열을 돌며 값을 하나로 모은다. (합계, 현재 항목) → 새 합계. 0은 시작값. C# LINQ의 Aggregate와 같다
  const totalBalance = accountsMock.reduce((sum, account) => sum + account.balance, 0)
  return { totalBalance, accounts: accountsMock }
}
