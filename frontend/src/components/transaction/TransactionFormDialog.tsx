import { useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect, NativeSelectOption } from '@/components/ui/native-select'
import type { Account } from '@/types/asset'
import type { Category, CategoryType } from '@/types/category'
import type { NewTransaction } from '@/types/transaction'

interface TransactionFormDialogProps {
  // 열림/닫힘은 부모(페이지)가 state로 가진다. 닫기 버튼·바깥 클릭·Esc를 누르면 onOpenChange(false)가 호출된다.
  open: boolean
  onOpenChange: (open: boolean) => void
  categories: Category[]
  accounts: Account[]
  // 저장 처리는 부모가 한다. 컴포넌트는 service를 직접 부르지 않고 입력값만 넘긴다 (계획서 6장 폴더 규칙).
  onSubmit: (input: NewTransaction) => Promise<void>
}

function TransactionFormDialog({ open, onOpenChange, ...formProps }: TransactionFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>거래 등록</DialogTitle>
          <DialogDescription>수입 또는 지출 내역을 입력하세요.</DialogDescription>
        </DialogHeader>
        {/* DialogContent는 닫히면 화면에서 사라진다(unmount). 그래서 폼 state를 아래 컴포넌트에 두면
            다시 열 때마다 새로 만들어져 입력칸이 자동으로 비워진다. */}
        <TransactionForm {...formProps} onCancel={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

interface TransactionFormProps {
  categories: Category[]
  accounts: Account[]
  onSubmit: (input: NewTransaction) => Promise<void>
  onCancel: () => void
}

function TransactionForm({ categories, accounts, onSubmit, onCancel }: TransactionFormProps) {
  const [type, setType] = useState<CategoryType>('Expense')
  const [date, setDate] = useState(getToday)
  // select·input의 값은 항상 문자열이다. 그래서 id도 문자열로 들고 있다가 저장할 때 숫자로 바꾼다. ''는 "선택 안 함"
  const [categoryId, setCategoryId] = useState('')
  const [accountId, setAccountId] = useState('')
  const [amount, setAmount] = useState('')
  const [memo, setMemo] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  // 지금 고른 종류(수입/지출)의 카테고리만 보여준다 (파생 값)
  const categoryOptions = categories.filter((c) => c.type === type)

  function changeType(nextType: CategoryType) {
    setType(nextType)
    // 수입 ↔ 지출을 바꾸면 이전에 고른 카테고리는 맞지 않으므로 비운다
    setCategoryId('')
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    try {
      await onSubmit({
        date,
        type,
        accountId: Number(accountId),
        categoryId: Number(categoryId),
        // 금액은 표시·전달만 한다. 계산은 Backend decimal에서 한다 (결정 D4)
        amount: Number(amount),
        // trim(): 앞뒤 공백 제거. 비어 있으면 memo를 보내지 않는다(undefined)
        memo: memo.trim() || undefined,
      })
    } finally {
      // finally: 성공·실패와 상관없이 마지막에 실행된다 (C#의 finally와 같다)
      setIsSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {/* 수입/지출 선택. aria-pressed: 화면낭독기에 "눌린 상태"인지 알린다 */}
      <div className="grid grid-cols-2 gap-2">
        <Button
          type="button"
          variant={type === 'Expense' ? 'default' : 'outline'}
          aria-pressed={type === 'Expense'}
          onClick={() => changeType('Expense')}
        >
          지출
        </Button>
        <Button
          type="button"
          variant={type === 'Income' ? 'default' : 'outline'}
          aria-pressed={type === 'Income'}
          onClick={() => changeType('Income')}
        >
          수입
        </Button>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="date">날짜</Label>
        <Input id="date" type="date" required value={date} onChange={(event) => setDate(event.target.value)} />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="amount">금액</Label>
        {/* type="number" 대신 text + inputMode="numeric": 모바일에서 숫자 키패드가 뜨고,
            number 입력칸의 마우스 휠로 값이 바뀌는 문제·'e' 입력 허용 문제가 없다.
            pattern: 1 이상의 정수만 허용 (0으로 시작 불가). 맞지 않으면 브라우저가 제출을 막는다. */}
        <Input
          id="amount"
          inputMode="numeric"
          autoComplete="off"
          pattern="[1-9][0-9]*"
          maxLength={13}
          placeholder="0"
          required
          value={amount}
          // replace(/\D/g, ''): 숫자가 아닌 글자(\D)를 모두(g) 지운다
          onChange={(event) => setAmount(event.target.value.replace(/\D/g, ''))}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="category">카테고리</Label>
        <NativeSelect
          id="category"
          className="w-full"
          required
          value={categoryId}
          onChange={(event) => setCategoryId(event.target.value)}
        >
          {/* value=""인 첫 옵션 + required: 아무것도 안 고르면 제출이 막힌다 */}
          <NativeSelectOption value="">선택하세요</NativeSelectOption>
          {categoryOptions.map((c) => (
            <NativeSelectOption key={c.id} value={c.id}>
              {c.name}
            </NativeSelectOption>
          ))}
        </NativeSelect>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="account">계좌</Label>
        <NativeSelect
          id="account"
          className="w-full"
          required
          value={accountId}
          onChange={(event) => setAccountId(event.target.value)}
        >
          <NativeSelectOption value="">선택하세요</NativeSelectOption>
          {accounts.map((a) => (
            <NativeSelectOption key={a.id} value={a.id}>
              {a.name}
            </NativeSelectOption>
          ))}
        </NativeSelect>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="memo">메모</Label>
        <Input
          id="memo"
          autoComplete="off"
          maxLength={100}
          placeholder="선택 입력"
          value={memo}
          onChange={(event) => setMemo(event.target.value)}
        />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          취소
        </Button>
        <Button type="submit" disabled={isSaving}>
          {isSaving ? '저장 중...' : '저장'}
        </Button>
      </DialogFooter>
    </form>
  )
}

// 오늘 날짜를 'YYYY-MM-DD'로. toISOString()은 UTC 기준이라 한국 오전 9시 전에는 어제 날짜가 나오므로 직접 만든다.
function getToday(): string {
  const today = new Date()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')
  return `${today.getFullYear()}-${month}-${day}`
}

export default TransactionFormDialog
