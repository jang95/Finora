import MoneyText from '@/components/common/MoneyText'
import type { Transaction } from '@/types/transaction'
import { formatDate } from '@/utils/format'

interface TransactionRowProps {
  transaction: Transaction
  // 있으면 누를 수 있는 줄(button)이 된다. 대시보드처럼 보기만 하는 곳에서는 넘기지 않는다.
  onClick?: () => void
  // 날짜 묶음 제목이 없는 곳(대시보드 최근 거래)에서는 줄마다 날짜를 보여준다
  showDate?: boolean
}

// 거래 한 줄. 가계부 목록과 대시보드 최근 거래에서 같이 쓴다.
function TransactionRow({ transaction: t, onClick, showDate = false }: TransactionRowProps) {
  const isTransfer = t.type === 'Transfer'
  // 날짜 · 계좌(이체면 보내는→받는) · 메모 중 있는 것만 · 로 이어 붙인다.
  // filter(Boolean): 빈 값(undefined, '')을 걸러낸다
  const detail = [
    showDate && formatDate(t.date),
    isTransfer ? `${t.accountName} → ${t.toAccountName}` : t.accountName,
    t.memo,
  ]
    .filter(Boolean)
    .join(' · ')

  const content = (
    <>
      <div className="min-w-0">
        <p className="font-medium">{isTransfer ? '이체' : t.categoryName}</p>
        {/* truncate: 너무 길면 한 줄에서 ...으로 자른다 (모바일 대비) */}
        <p className="truncate text-sm text-muted-foreground">{detail}</p>
      </div>
      <MoneyText
        amount={t.amount}
        tone={t.type === 'Income' ? 'income' : t.type === 'Expense' ? 'expense' : undefined}
        className="shrink-0 font-semibold"
      />
    </>
  )
  const className = 'flex w-full items-center justify-between gap-4 px-4 py-3 text-left'

  // 누를 수 있는 것은 div가 아니라 button으로 만든다. 그래야 키보드(Tab, Enter)로도 선택되고 화면낭독기가 "버튼"이라고 읽는다.
  return onClick ? (
    <button type="button" className={`${className} hover:bg-muted/50`} onClick={onClick}>
      {content}
    </button>
  ) : (
    <div className={className}>{content}</div>
  )
}

export default TransactionRow
