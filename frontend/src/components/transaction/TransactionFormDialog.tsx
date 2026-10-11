import { useState, type ComponentProps, type FormEvent } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
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
import type { Category } from '@/types/category'
import type { NewTransaction, Transaction, TransactionType } from '@/types/transaction'
import { getToday } from '@/utils/yearMonth'

// 종류 선택 버튼 목록. 배열로 두고 map으로 그리면 버튼 3개를 같은 코드로 만들 수 있다.
// 컴포넌트 함수 밖에 둔 이유: 안에 두면 그릴 때마다 같은 배열을 새로 만든다. 바뀌지 않는 값은 밖에 한 번만 만든다.
// (이렇게 고정된 상수는 이름을 대문자_밑줄로 써서 구분하는 관례가 있다)
const TYPE_OPTIONS: { value: TransactionType; label: string }[] = [
  { value: 'Expense', label: '지출' },
  { value: 'Income', label: '수입' },
  { value: 'Transfer', label: '이체' },
]

interface TransactionFormDialogProps {
  // 열림/닫힘은 부모(페이지)가 state로 가진다. 닫기 버튼·바깥 클릭·Esc를 누르면 onOpenChange(false)가 호출된다.
  open: boolean
  onOpenChange: (open: boolean) => void
  // 수정할 거래. null이면 새로 등록한다. (등록/수정 폼이 거의 같아서 컴포넌트 하나로 처리한다)
  transaction: Transaction | null
  // 새로 등록할 때 날짜 칸의 처음 값. 없으면 오늘 (달력에서 고른 날짜, 보고 있는 달 등 부모가 정한다)
  defaultDate?: string
  categories: Category[]
  accounts: Account[]
  // 저장·삭제 처리는 부모가 한다. 컴포넌트는 service를 직접 부르지 않고 입력값만 넘긴다 (계획서 6장 폴더 규칙).
  // [함수 타입] (매개변수: 타입) => 반환 타입. "NewTransaction을 받아서 Promise<void>를 돌려주는 함수"를 props로 받는다.
  //   C#의 Func<NewTransaction, Task>와 같다. Promise<void>는 "끝나는 시점만 알려주고 값은 없음" (C#의 Task)
  onSubmit: (input: NewTransaction) => Promise<void>
  onDelete: () => Promise<void>
}

function TransactionFormDialog({ open, onOpenChange, ...formProps }: TransactionFormDialogProps) {
  const isEdit = formProps.transaction !== null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? '거래 수정' : '거래 등록'}</DialogTitle>
          <DialogDescription>수입·지출 또는 내 계좌 간 이체 내역을 입력하세요.</DialogDescription>
        </DialogHeader>
        {/* DialogContent는 닫히면 화면에서 사라진다(unmount). 그래서 폼 state를 아래 컴포넌트에 두면
            다시 열 때마다 새로 만들어져, 등록이면 빈 칸으로 / 수정이면 그 거래 값으로 다시 채워진다. */}
        <TransactionForm {...formProps} onCancel={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

interface TransactionFormProps {
  transaction: Transaction | null
  defaultDate?: string
  categories: Category[]
  accounts: Account[]
  onSubmit: (input: NewTransaction) => Promise<void>
  onDelete: () => Promise<void>
  onCancel: () => void
}

function TransactionForm({
  transaction,
  defaultDate,
  categories,
  accounts,
  onSubmit,
  onDelete,
  onCancel,
}: TransactionFormProps) {
  // useState의 초기값은 처음 만들어질 때 한 번만 쓰인다. 수정이면 기존 거래 값으로 시작한다.
  const [type, setType] = useState<TransactionType>(transaction?.type ?? 'Expense')
  // () => ...: 함수로 넘기면 처음 한 번만 계산한다 (getToday()를 그릴 때마다 부르지 않게)
  const [date, setDate] = useState(() => transaction?.date ?? defaultDate ?? getToday())
  // select·input의 값은 항상 문자열이다. 그래서 id도 문자열로 들고 있다가 저장할 때 숫자로 바꾼다. ''는 "선택 안 함"
  // transaction?.categoryId?.toString() ?? '' 읽는 법 (?. 와 ?? 는 transactionService 설명 참고):
  //   등록이면(transaction이 null) → undefined → '' / 이체라 categoryId가 없으면 → undefined → ''
  //   둘 다 있으면 → 3 → '3' (toString(): 숫자를 문자열로)
  const [categoryId, setCategoryId] = useState(transaction?.categoryId?.toString() ?? '')
  const [accountId, setAccountId] = useState(transaction?.accountId.toString() ?? '')
  const [toAccountId, setToAccountId] = useState(transaction?.toAccountId?.toString() ?? '')
  const [amount, setAmount] = useState(transaction?.amount.toString() ?? '')
  const [memo, setMemo] = useState(transaction?.memo ?? '')
  const [isSaving, setIsSaving] = useState(false)

  const isTransfer = type === 'Transfer'
  // 지금 고른 종류(수입/지출)의 카테고리만 보여준다 (파생 값). 이체는 카테고리가 없다.
  const categoryOptions = categories.filter((c) => c.type === type)
  // 같은 계좌로 이체할 수는 없다. 둘 다 고른 뒤에만 오류로 본다 (고르는 중에는 빈 값이라 같을 수 있음)
  const isSameAccount = isTransfer && accountId !== '' && accountId === toAccountId

  function changeType(nextType: TransactionType) {
    setType(nextType)
    // 종류를 바꾸면 이전에 고른 카테고리는 맞지 않으므로 비운다
    setCategoryId('')
  }

  // [async / await] C#과 같은 개념이다.
  // - async 함수 안에서 await를 붙이면, 그 Promise가 끝날 때까지 다음 줄로 넘어가지 않고 기다린다.
  // - 기다리는 동안 화면이 멈추지 않는다 (브라우저는 다른 일을 계속 한다).
  // - await한 작업이 실패(throw)하면 그 자리에서 에러가 나서 try/catch/finally로 잡을 수 있다.
  //   .then()으로 쓴 DashboardPage와 같은 일을 하지만, 위에서 아래로 읽혀서 순서가 있는 작업에 편하다.
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    // 화면 검사는 편의용이다. 최종 검사(내 계좌인지, 같은 계좌인지)는 Backend가 한다.
    if (isSameAccount) return
    // 저장 중에는 버튼을 꺼서(disabled) 두 번 눌러 같은 거래가 두 번 저장되는 것을 막는다.
    // 실제 API는 응답까지 시간이 걸려서, 그 사이 사용자가 다시 누를 수 있기 때문이다.
    setIsSaving(true)
    try {
      await onSubmit({
        date,
        type,
        accountId: Number(accountId),
        // 이체면 받는 계좌만, 수입/지출이면 카테고리만 보낸다. 화면에 안 보이는 칸의 값이 섞여 저장되지 않게 한다.
        toAccountId: isTransfer ? Number(toAccountId) : undefined,
        categoryId: isTransfer ? undefined : Number(categoryId),
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

  async function handleDelete() {
    setIsSaving(true)
    try {
      await onDelete()
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {/* 지출/수입/이체 선택. aria-pressed: 화면낭독기에 "눌린 상태"인지 알린다 */}
      <div className="grid grid-cols-3 gap-2">
        {TYPE_OPTIONS.map((option) => (
          <Button
            key={option.value}
            type="button"
            variant={type === option.value ? 'default' : 'outline'}
            aria-pressed={type === option.value}
            onClick={() => changeType(option.value)}
          >
            {option.label}
          </Button>
        ))}
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

      {/* 이체가 아닐 때만 카테고리. 화면에서 빠진 칸은 required 검사도 하지 않는다
          [조건부 렌더링] {조건 && (화면)}: 조건이 true면 화면을 그리고, false면 아무것도 그리지 않는다.
            둘 중 하나를 그릴 때는 {조건 ? (A) : (B)} 를 쓴다 (아래 Label 참고).
            주의: 조건이 숫자 0이면 0이 그대로 화면에 찍힌다. 그래서 조건은 true/false 값으로 쓴다. */}
      {!isTransfer && (
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
      )}

      <div className="flex flex-col gap-2">
        {/* 이체면 "보내는 계좌", 아니면 "계좌" */}
        <Label htmlFor="account">{isTransfer ? '보내는 계좌' : '계좌'}</Label>
        <AccountSelect id="account" accounts={accounts} value={accountId} onChange={setAccountId} />
      </div>

      {isTransfer && (
        <div className="flex flex-col gap-2">
          <Label htmlFor="toAccount">받는 계좌</Label>
          {/* aria-invalid / aria-describedby 설명은 RegisterPage의 비밀번호 확인 칸 참고 */}
          <AccountSelect
            id="toAccount"
            accounts={accounts}
            value={toAccountId}
            onChange={setToAccountId}
            aria-invalid={isSameAccount}
            aria-describedby="toAccountError"
          />
          {isSameAccount && (
            <p id="toAccountError" className="text-sm text-destructive">
              보내는 계좌와 받는 계좌가 같습니다.
            </p>
          )}
        </div>
      )}

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
        {/* 수정일 때만 삭제 버튼. sm:mr-auto: PC에서 왼쪽 끝으로 밀어 저장 버튼과 떨어뜨린다 (실수로 누르지 않게) */}
        {transaction !== null && (
          // AlertDialog: 되돌릴 수 없는 작업 전에 한 번 더 묻는 확인 창. Trigger 버튼을 누르면 열린다.
          // 열림 state를 직접 만들지 않았다 (거래 팝업과 다름). 부모가 알 필요가 없으면 컴포넌트가 알아서 관리하게 둔다.
          // 확인(Action)·취소(Cancel)를 누르면 자동으로 닫힌다.
          <AlertDialog>
            {/* asChild: Trigger가 자기 버튼을 따로 만들지 않고, 안쪽 자식(<Button>)에 "누르면 열기" 기능만 붙인다.
                없으면 <button> 안에 <button>이 들어가는 잘못된 HTML이 되고, 우리 Button 모양도 쓸 수 없다. */}
            <AlertDialogTrigger asChild>
              <Button type="button" variant="destructive" className="sm:mr-auto" disabled={isSaving}>
                삭제
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>이 거래를 삭제할까요?</AlertDialogTitle>
                <AlertDialogDescription>삭제한 거래는 되돌릴 수 없습니다.</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>취소</AlertDialogCancel>
                <AlertDialogAction variant="destructive" onClick={handleDelete}>
                  삭제
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        )}
        <Button type="button" variant="outline" onClick={onCancel}>
          취소
        </Button>
        <Button type="submit" disabled={isSaving || isSameAccount}>
          {isSaving ? '저장 중...' : '저장'}
        </Button>
      </DialogFooter>
    </form>
  )
}

// 계좌 선택 칸. "보내는 계좌"와 "받는 계좌"에 같은 모양이 두 번 필요해서 작은 컴포넌트로 뺐다.
// ComponentProps<typeof NativeSelect>: NativeSelect가 받는 props 타입을 그대로 가져온다.
// Omit으로 value/onChange만 빼고 우리 방식(문자열 값을 바로 넘김)으로 다시 정의한다. 나머지(id, aria-*)는 그대로 전달.
// [교집합 타입] A & B: A의 속성과 B의 속성을 모두 가진 타입. interface의 extends와 비슷하다.
//   interface는 Omit<...> 같은 계산된 타입을 이어 붙이기 불편해서 여기서는 type + & 를 썼다.
type AccountSelectProps = Omit<ComponentProps<typeof NativeSelect>, 'value' | 'onChange'> & {
  accounts: Account[]
  value: string
  onChange: (value: string) => void
}

// ...rest: 위에서 꺼내고 남은 나머지 props를 한 객체로 모은다. 그대로 NativeSelect에 넘긴다.
function AccountSelect({ accounts, value, onChange, ...rest }: AccountSelectProps) {
  return (
    <NativeSelect
      {...rest}
      className="w-full"
      required
      value={value}
      onChange={(event) => onChange(event.target.value)}
    >
      <NativeSelectOption value="">선택하세요</NativeSelectOption>
      {accounts.map((a) => (
        <NativeSelectOption key={a.id} value={a.id}>
          {a.name}
        </NativeSelectOption>
      ))}
    </NativeSelect>
  )
}

export default TransactionFormDialog
