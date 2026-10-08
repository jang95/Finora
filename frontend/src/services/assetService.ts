import { accountsMock } from '@/mocks/accounts'
import type { Account } from '@/types/asset'

// Backend가 생기면 GET /api/accounts 호출로 바꾼다.
export async function getAccounts(): Promise<Account[]> {
  return accountsMock
}
