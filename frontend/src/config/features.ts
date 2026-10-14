// 기능 스위치. 아직 만들지 않은 기능의 화면 코드는 지우지 않고, 여기서 꺼 두었다가 그 Phase에서 켠다.
// as const: 값을 고정(읽기 전용)으로 만든다. 다른 곳에서 실수로 바꿀 수 없다.
export const FEATURES = {
  // 계좌(자산)·이체. Phase 8에서 Backend가 생기면 true로 바꾼다. (결정 D16)
  //   false일 때: 거래 입력 창의 계좌 선택·이체 버튼 숨김, 자산 메뉴는 "준비 중" 화면
  accounts: false,
} as const
