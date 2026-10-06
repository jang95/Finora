# Finora

가족·지인용 개인 금융관리 웹 서비스 (가계부 / 자산 / 투자). 사용자별 데이터 완전 분리가 핵심.

- 전체 계획·결정 기록·진행 현황: `docs/development-plan.md` — 작업 시작 전 13장(진행 현황)을 확인한다.

## 사용자와 작업 방식

- 사용자는 React/TypeScript/C#을 배우면서 개발 중이다. 새 개념이 처음 나오면 Finora 코드 기준으로 짧게 설명한다.
- 한 번에 작은 단위로 만든다. 여러 파일을 한꺼번에 대량 생성하지 않는다.
- 응답은 한국어로 한다.
- 라이브러리는 계획서의 해당 Phase가 됐을 때만 추가한다.

## 구조

- 현재: Vite 프로젝트가 저장소 루트에 있음 (Phase 0에서 `frontend/`로 이동 예정)
- 목표: `frontend/` (React+TS+Vite), `backend/` (ASP.NET Core, .NET 10), `docs/`
- Frontend 명령어: `npm run dev` / `npm run lint` / `npm run build`

## 반드시 지킬 규칙

- **UserId는 항상 Backend의 인증 정보에서 가져온다.** 요청 Body/URL의 UserId를 신뢰하지 않는다. 모든 조회/수정/삭제에서 소유권을 확인한다.
- **금액 계산은 Backend `decimal`** (DB `numeric`). float/double 금지. Frontend는 표시만 한다.
- 화면(pages/components)에서 `fetch`를 직접 호출하지 않는다. `services/`를 거친다.
- 비밀번호·접속 문자열·API Key를 코드나 Git에 넣지 않는다.

## 문서 관리

- 계획/설계 문서는 `docs/`에 둔다. 파일명은 영문 kebab-case, 내용은 한국어.
- Phase가 끝나면 `docs/development-plan.md` 13장(진행 현황)을 갱신한다.
- 기술 결정을 바꾸면 계획서 3장 "주요 결정 기록"에 이유와 함께 추가한다.
- 이 파일은 짧게 유지한다. 세부 내용은 `docs/`나 `frontend/CLAUDE.md`, `backend/CLAUDE.md`로 분리한다.
