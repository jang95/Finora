import ComingSoon from '@/components/common/ComingSoon'

// 투자 기능은 첫 배포 이후(Phase 10)에 만든다. 그때까지 메뉴는 남겨두고 안내만 보여준다. (결정 D10)
function InvestmentsPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">투자</h1>
      <ComingSoon
        title="투자 관리는 준비 중입니다."
        description="가계부 기능을 먼저 제공하고, 이후에 추가될 예정입니다."
      />
    </div>
  )
}

export default InvestmentsPage
