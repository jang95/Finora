import { useState, type FormEvent } from 'react'
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
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import type { Category } from '@/types/category'

interface CategorySectionProps {
  title: string
  categories: Category[]
  // 저장·삭제는 부모(SettingsPage)가 service로 처리한다. 실패하면 에러를 던지고, 여기서 받아 메시지를 보여준다.
  onAdd: (name: string) => Promise<void>
  onRename: (id: number, name: string) => Promise<void>
  onDelete: (id: number) => Promise<void>
}

// 수입 또는 지출 카테고리 한 묶음: 목록 + 이름 변경 + 삭제 + 추가
function CategorySection({ title, categories, onAdd, onRename, onDelete }: CategorySectionProps) {
  const [newName, setNewName] = useState('')
  // 이름을 고치는 중인 카테고리 id. null이면 고치는 중인 것이 없다 (한 번에 하나만 고친다)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingName, setEditingName] = useState('')
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  // 추가·변경·삭제가 모두 "저장 중 표시 → 실행 → 실패하면 메시지"로 같아서 한 함수로 묶었다.
  // action: 실행할 작업을 함수로 받는다 (C#에서 Func<Task>를 넘기는 것과 같다)
  async function run(action: () => Promise<void>): Promise<boolean> {
    setError('')
    setIsSaving(true)
    try {
      await action()
      return true
    } catch (e) {
      // [catch의 e] JS는 Error가 아닌 값도 throw할 수 있어서 e의 타입은 unknown이다.
      //   instanceof Error로 확인한 뒤에야 e.message를 쓸 수 있다 (C#의 catch (Exception e)와 다른 점)
      setError(e instanceof Error ? e.message : '저장하지 못했습니다.')
      return false
    } finally {
      setIsSaving(false)
    }
  }

  async function handleAdd(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const name = newName.trim()
    // required는 공백만 입력해도 통과시키므로 여기서 한 번 더 확인한다
    if (name === '') {
      setError('카테고리 이름을 입력하세요.')
      return
    }
    if (await run(() => onAdd(name))) setNewName('')
  }

  function startEdit(category: Category) {
    setError('')
    setEditingId(category.id)
    setEditingName(category.name)
  }

  // 취소하면 고치던 중에 난 오류 문구도 같이 지운다
  function cancelEdit() {
    setEditingId(null)
    setError('')
  }

  async function handleRename(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const name = editingName.trim()
    if (editingId === null) return
    if (name === '') {
      setError('카테고리 이름을 입력하세요.')
      return
    }
    if (await run(() => onRename(editingId, name))) setEditingId(null)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {categories.length === 0 ? (
          <p className="text-sm text-muted-foreground">카테고리가 없습니다.</p>
        ) : (
          <ul className="divide-y rounded-md border">
            {categories.map((category) => (
              <li key={category.id} className="flex items-center gap-2 px-3 py-2">
                {editingId === category.id ? (
                  // 고치는 중인 줄만 입력칸으로 바꾼다. Enter로 저장된다 (form 안의 submit 버튼)
                  <form onSubmit={handleRename} className="flex flex-1 items-center gap-2">
                    <Input
                      aria-label="카테고리 이름"
                      autoFocus
                      required
                      maxLength={20}
                      value={editingName}
                      onChange={(event) => setEditingName(event.target.value)}
                    />
                    <Button type="submit" size="sm" disabled={isSaving}>
                      저장
                    </Button>
                    <Button type="button" size="sm" variant="ghost" onClick={cancelEdit}>
                      취소
                    </Button>
                  </form>
                ) : (
                  <>
                    {/* flex-1: 남은 폭을 모두 차지해서 버튼들을 오른쪽 끝으로 민다 */}
                    <span className="flex-1 truncate">{category.name}</span>
                    <Button size="sm" variant="ghost" onClick={() => startEdit(category)}>
                      수정
                    </Button>
                    {/* 삭제 확인 창 설명은 TransactionFormDialog 참고 */}
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button size="sm" variant="ghost" className="text-destructive" disabled={isSaving}>
                          삭제
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>'{category.name}' 카테고리를 삭제할까요?</AlertDialogTitle>
                          <AlertDialogDescription>거래에서 사용 중인 카테고리는 삭제할 수 없습니다.</AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>취소</AlertDialogCancel>
                          <AlertDialogAction variant="destructive" onClick={() => run(() => onDelete(category.id))}>
                            삭제
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </>
                )}
              </li>
            ))}
          </ul>
        )}

        {/* role="alert": 메시지가 나타나면 화면낭독기가 바로 읽어준다 */}
        {error && (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        )}

        <form onSubmit={handleAdd} className="flex gap-2">
          <Input
            aria-label={`새 ${title} 이름`}
            placeholder="새 카테고리 이름"
            required
            maxLength={20}
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
          />
          <Button type="submit" disabled={isSaving} className="shrink-0">
            추가
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

export default CategorySection
