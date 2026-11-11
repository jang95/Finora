# Finora 개인 금융관리 프로그램 개발 계획서

> 기준일: 2026-11-11  
> 현재 상태: Phase 7.5 진행 중 (사용 피드백 반영). 1번 카테고리 3단계 완료, 2번 저금은 Backend 테이블까지 만들고 잠시 멈춤, 3번 통계 대시보드 진행 중

### 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-11-11 | v14 — 통계 화면을 위젯 대시보드(그래프 11개, 드래그로 배치, 사용자별 저장)로 전면 개편 (결정 D26, D27). 저금(②)은 Backend 테이블까지 만든 상태에서 잠시 멈추고 통계 대시보드(③)를 먼저 한다 |
| 2026-11-05 | v13 — Phase 7.5에 저금(자유저금·적금·예금) 추가 (결정 D24). 모바일 하단 탭의 투자를 저금으로 바꿈 (결정 D25) |
| 2026-10-29 | v12 — Phase 7 완료. Phase 8 전에 실제 사용 피드백을 반영하는 Phase 7.5 추가 (결정 D22). 카테고리 3단계 계층 (결정 D23) |
| 2026-10-21 | v11 — 노트북 운영체제 Ubuntu Server → Linux Mint 22.3 Xfce(D21). 노트북 준비 안내(`docs/laptop-setup.md`) |
| 2026-10-15 | v10 — 첫 배포 방식: 클라우드 VM(D19) → 집 노트북 + Tailscale Funnel(D20). Phase 7 작업 세분화, React 빌드 파일 제공(7-1) |
| 2026-10-14 | v9 — Phase 6 완료 (기간 조회 일·주·월·연·직접 지정, 대시보드·통계 실제 데이터 연결, 비밀번호 변경). 결정 D18(한국 시간 기준) 추가 |
| 2026-10-14 | v8 — Phase 5 완료 (카테고리 관리, 거래 등록·수정·삭제, 월별·연간 조회, TanStack Query) |
| 2026-10-14 | v7 — 계좌·이체를 Phase 8로 미루고 가계부 중심으로 첫 배포 (결정 D16). 총자산 2종류(가계부 기준·계좌 기준)와 일·주·월·연·직접 지정 기간 조회 (결정 D17) |
| 2026-10-13 | v6 — DB 테스트는 Testcontainers (결정 D13), 초대코드는 서버 명령어로 발급 (결정 D14), Categories를 Phase 4에서 먼저 생성 (결정 D15). Phase 4 완료 |
| 2026-10-13 | v5 — 개발용 PostgreSQL을 Docker로 실행 (결정 D12). Phase 3 완료 |
| 2026-10-12 | v4 — 가계부 화면을 목록·달력·월별 탭으로 구성 (결정 D11). Phase 5 API 예시를 Phase 2 화면 응답 형태에 맞춤. Phase 2 완료 |
| 2026-10-08 | v3 — 투자(주식)를 첫 배포 이후로 미룸. 가계부 + 계좌 기본 + 대시보드/통계/설정을 먼저 완성하고 첫 배포 (결정 D10) |
| 2026-10-07 | v2 — 저장소 구조 확정(monorepo), 인증 방식 확정, 인증을 실제 데이터 저장보다 앞으로 이동, Backend+DB 단계 통합, 첫 배포 단계 추가, DB 설계 보강(금액 타입·이체·통화·자산 이력), 단계별 완료 기준 추가, 문서 관리 규칙 추가 |
| 2026-10-07 | v1 — 최초 작성 |

---

## 1. 프로젝트 목표

Finora는 가족·지인 몇 명(10명 이하)이 사용할 수 있는 **개인 금융관리 서비스**다.

- 각 사용자는 자신의 ID/비밀번호로 로그인한다.
- 사용자는 자신의 가계부, 자산, 투자정보만 관리한다.
- 다른 사용자의 데이터는 볼 수 없다.
- 아무나 가입할 수 없다. **초대코드가 있어야 가입**할 수 있다.
- Windows와 Android에서 사용할 수 있도록 웹/PWA를 우선 개발한다.
- 이후 필요하면 Android 네이티브 앱으로 확장한다.

### 핵심 원칙

**"여러 사람이 사용하는 하나의 서비스지만, 금융 데이터는 사용자별로 완전히 분리한다."**

---

## 2. 시스템 구조

```text
[ Windows / Android 사용자 (브라우저 / PWA) ]
             │
             │ HTTPS (같은 도메인)
             ▼
     ┌──────────────────────────────┐
     │ ASP.NET Core                 │
     │  ├─ React 빌드 결과 제공      │  ← wwwroot (정적 파일)
     │  └─ /api/*  Web API          │  ← C# Backend
     └───────┬───────────────┬──────┘
             │               │
             ▼               ▼
     ┌──────────────┐  ┌──────────────────┐
     │ PostgreSQL   │  │ 외부 금융 API     │
     │ Database     │  │ 주가 / 환율 등    │
     └──────────────┘  └──────────────────┘
```

외부 금융 API는 **Backend만 호출**한다. API Key가 브라우저에 노출되면 안 되기 때문이다.

### 개발 환경 vs 운영 환경

| 구분 | Frontend | Backend | 연결 방식 |
|---|---|---|---|
| 개발 | `npm run dev` (Vite, :5173) | `dotnet watch` (:5xxx) | Vite proxy가 `/api` 요청을 Backend로 전달 |
| 운영 | `npm run build` 결과물 | ASP.NET Core | Backend가 React 빌드 파일을 함께 제공 |

이렇게 하면 운영 환경에서 Frontend와 API가 **같은 주소(same-origin)** 가 되어, CORS 설정이 필요 없고 Cookie 인증이 단순해진다.

---

## 3. 기술 스택 및 주요 결정

| 영역 | 기술 | 도입 시점 |
|---|---|---|
| Frontend | React 19 + TypeScript | 완료 |
| Frontend Build | Vite | 완료 |
| Code Lint | ESLint | 완료 |
| 라우팅 | React Router | 완료 |
| 스타일 | Tailwind CSS v4 + shadcn/ui (Radix, lucide 아이콘), 글꼴 Pretendard | 완료 |
| 서버 데이터 관리 | TanStack Query | Phase 5 |
| 차트 | Recharts | Phase 2 (통계 화면) |
| 드래그 앤 드롭 (통계 위젯 배치) | @dnd-kit/core + @dnd-kit/sortable | Phase 7.5 (결정 D26) |
| 달력 (기간 직접 지정) | shadcn Calendar (react-day-picker, date-fns) | Phase 6 |
| Backend | ASP.NET Core Web API (.NET 10 LTS, C#) | Phase 3 |
| ORM | Entity Framework Core + Npgsql | Phase 3 |
| Database | PostgreSQL | Phase 3 |
| 인증 | ASP.NET Core Identity + HttpOnly Cookie | Phase 4 |
| PC | Web / PWA | — |
| Android | PWA 우선, 필요 시 React Native 검토 | Phase 9 |
| PWA | vite-plugin-pwa | Phase 9 |
| 외부 데이터 | 주식/환율 금융 API | Phase 11 |

라이브러리는 **필요한 단계가 왔을 때** 설치한다. 미리 깔아두지 않는다.

### 주요 결정 기록

결정을 바꿀 때는 이 표에 이유와 함께 추가한다.

| # | 결정 | 이유 |
|---|---|---|
| D1 | 인증은 JWT가 아니라 **Cookie (HttpOnly, Secure, SameSite)** | 같은 도메인에서 서비스하므로 Cookie가 가장 단순하고 안전하다. JWT를 localStorage에 저장하면 XSS 시 탈취 위험이 있다. ASP.NET Core Identity가 비밀번호 Hash, 계정 잠금을 기본 제공한다. |
| D2 | 저장소는 **하나(monorepo)**: `frontend/`, `backend/`, `docs/` | 혼자 개발하는 작은 프로젝트에서 저장소를 나누면 관리 비용만 늘어난다. |
| D3 | 회원가입은 **초대코드 방식** | 가족·지인 전용 서비스. 공개 가입을 열 이유가 없다. |
| D4 | 금액 계산은 **Backend의 `decimal`** 에서 한다 | JavaScript `number`는 부동소수점이라 외화·소수 주식 계산에서 오차가 생긴다. Frontend는 표시만 담당한다. |
| D5 | Phase 2의 임시 데이터는 **services 계층 안에** 둔다 | 화면 코드는 `transactionService.getList()`만 호출한다. Phase 5에서 service 내부만 실제 API 호출로 바꾸면 화면은 수정할 필요가 없다. |
| D6 | 인증을 실제 데이터 저장보다 **먼저** 만든다 | 사용자 분리가 없는 상태로 저장 기능을 만들면, 나중에 모든 API와 테이블을 다시 고쳐야 한다. |
| D7 | GitHub 저장소는 **Public** (`jang95/Finora`) | 이직·포트폴리오 제출용. 대신 비밀 정보·실제 금융 데이터·운영 서버 정보는 저장소에 절대 넣지 않는다 (9장 운영 원칙). |
| D8 | 스타일은 CSS Modules 대신 **Tailwind CSS v4 + shadcn/ui** | 사용자의 Tailwind 경험, 채용 시장 수요, 표·폼·모달·차트(Recharts 기반) 컴포넌트를 바로 쓸 수 있음. 화면이 2개뿐인 Phase 1 직후에 전환해 비용이 최소였다. |
| D9 | 투자 손익 색은 **한국식 (상승 빨강 / 하락 파랑)**, 가계부 수입/지출(초록/빨강)과 별도 토큰(`gain`/`loss`) | 사용자가 익숙한 국내 증권사 표시 방식. 수입/지출 토큰을 재사용하면 "이익 = 지출 색"처럼 의미가 꼬이므로 분리한다. |
| D10 | 투자(주식)는 **첫 배포 이후**로 미룬다. 첫 배포 범위는 가계부 + 계좌 기본 + 대시보드/통계/설정. 투자 메뉴는 "준비 중"으로 남긴다 | 가족이 매일 쓰는 기능은 가계부다. 투자는 평균매수가·환율·외부 시세 API까지 필요해 범위가 가장 크므로, 먼저 가계부 중심으로 배포해 피드백을 받는다. 계좌는 가계부 거래와 총자산에 필요해서 기본 관리만 앞에 둔다. (2026-10-08) |
| D11 | 가계부 화면은 **목록(일별) / 달력 / 월별** 탭 + 기간 요약으로 구성한다. 주별 탭·필터·예산은 Phase 6 이후 | 여러 가계부 앱을 조사한 결과, 직접 입력형 가계부의 공통 구성이 "일별 목록 + 달력 + 월별(12개월 표)"이었다. 연간 보기는 월별 표가 대신한다. Backend API(월 요약·일 소계·연간 표)의 응답 형태를 Phase 5 전에 확정하려고 Phase 2에서 화면을 먼저 만든다. (2026-10-12) |
| D12 | 개발용 PostgreSQL은 **Docker 컨테이너**로 실행한다 (저장소 루트 `compose.yaml`, 접속 정보는 커밋하지 않는 `.env`) | Windows에 직접 설치하는 것보다 지우고 다시 만들기 쉽고, 버전이 `compose.yaml`에 고정된다. 운영 서버 배포(Phase 7)에서도 같은 방식을 쓸 수 있다. 포트는 `127.0.0.1`에만 열어 이 PC에서만 접속된다. (2026-10-13) |
| D13 | DB가 필요한 자동 테스트(사용자 분리 등)는 **Testcontainers**로 테스트마다 실제 PostgreSQL을 띄워 실행한다 | SQLite/InMemory는 PostgreSQL과 동작이 달라(numeric, 대소문자, 제약 조건) 보안 핵심인 분리 테스트가 통과해도 운영에서 틀릴 수 있다. 개발용 DB와 섞이지 않고 매번 깨끗한 상태에서 시작한다. 대신 테스트 시 Docker가 실행 중이어야 한다. (2026-10-13) |
| D14 | 초대코드는 **서버 명령어로 발급**한다 (`dotnet run --project Finora.Api -- invite create`). 관리자 화면/API는 만들지 않는다 | 사용자가 10명 이하라 발급이 드물다. 웹에 발급 기능을 두면 관리자 역할 관리와 공격 대상이 늘어난다. 서버에 접속할 수 있는 사람만 발급할 수 있다. (2026-10-13) |
| D15 | 사용자 분리 테스트용 첫 사용자 소유 테이블로 **Categories를 Phase 4에서** 만든다 (테이블 + 가입 시 기본 카테고리 + 조회 API). 화면 연결과 나머지 CRUD는 Phase 5 | Phase 4 완료 기준(사용자 분리 자동 테스트)을 실제 데이터로 검증하려면 사용자 소유 테이블이 하나 필요하다. 가입 시 기본 카테고리 생성(8장)과 자연스럽게 이어진다. (2026-10-13) |
| D16 | **계좌(자산)와 이체는 첫 배포 이후**(Phase 8)로 미룬다. 가계부 거래는 수입/지출만으로 시작한다. Phase 2의 계좌·이체 화면 코드는 지우지 않고 남겨 둔다: 거래 입력 창의 계좌 선택·이체 버튼은 숨기고, 자산 메뉴와 대시보드의 계좌 기준 총자산은 "준비 중"으로 보여준다. 거래의 계좌(`accountId`)는 필수가 아니라 선택 항목으로 바꾸고, 계좌가 생기기 전 이체 요청은 Backend가 거절한다. D10의 첫 배포 범위에서 "계좌 기본"을 뺀다 | 가족이 매일 쓰는 것은 가계부이고, 계좌는 잔액 관리 방식(저장 vs 계산)·이체 규칙까지 정해야 해서 범위가 크다. 가계부만으로 먼저 배포해 피드백을 받는다. 계좌를 추가할 때 거래에 `AccountId`를 선택 컬럼으로 붙이면 되므로 그전에 입력한 거래는 "계좌 미지정"으로 남고 다시 입력할 필요가 없다. (2026-10-14) |
| D17 | 총자산은 **두 종류**: ① 가계부 기준(거래 전체의 수입 − 지출 누적) ② 계좌 기준(계좌 잔액 합계, Phase 8). 대시보드와 통계에서 **일간 / 주간(월~일) / 월간 / 연간 / 직접 지정 기간(달력)** 으로 조회한다. 기간을 고르면 그 기간의 수입·지출·합계와 기간 마지막 날 기준 누적 총자산을 보여준다 | 계좌 없이도 "지금까지 모은 돈"을 보고 싶다는 요구. 기간 조회는 D11에서 Phase 6 이후로 미뤘던 주별·기간 보기를 대시보드·통계에서 해결한다. 집계는 Backend decimal로 한다 (D4). (2026-10-14) |
| D18 | 서버가 "오늘"을 정해야 할 때는 **한국 시간(Asia/Seoul)** 기준으로 한다 (`KoreaTime`, `TimeProvider`). 기간 조회는 화면이 고른 from/to를 보낸다 | 배포 서버는 UTC일 수 있어서, UTC로 정하면 한국 1일 오전 0~9시가 전달로 계산된다. 사용자는 모두 한국에 있다. 대시보드도 기간 선택을 쓰게 되면서 지금은 쓰는 곳이 없고, Phase 8 월말 스냅샷에서 쓴다. 시계를 `TimeProvider`로 받아 경계 시각을 테스트로 확인한다. (2026-10-14) |
| D19 | (→ D20으로 변경) 첫 배포는 **클라우드 VM 1대 + 도메인**(10장 방식 B). VM 안에서 Docker Compose로 ASP.NET Core(React 빌드 포함) + PostgreSQL을 띄우고, 앞에 **Caddy**(리버스 프록시, HTTPS 인증서 자동 발급·갱신)를 둔다. VM 업체와 도메인은 서버를 만들 때 정한다 | 가족이 앱 설치 없이 주소만 열면 쓸 수 있어야 한다(A는 사용자마다 Tailscale 설치 필요). C(PaaS)보다 비용이 낮다. 개발 DB도 Docker라(D12) 같은 구성을 그대로 쓸 수 있다. Caddy는 설정 몇 줄로 HTTPS가 되어 인증서 관리 부담이 적다. (2026-10-15) |
| D20 | 첫 배포는 **집의 안 쓰는 노트북**(Ubuntu Server LTS로 교체) + Docker Compose(ASP.NET Core + PostgreSQL) + **Tailscale Funnel**로 공개한다. 도메인은 사지 않고 Tailscale이 주는 `*.ts.net` 주소와 HTTPS를 쓴다. Caddy는 쓰지 않는다. DB 백업은 반드시 노트북 밖으로 복사한다 | D19(클라우드 VM)는 연 13~14만 원이 들지만, 노트북은 전기료만 든다. Funnel은 공유기 포트를 열지 않고 집 IP도 드러나지 않으며, 가족은 앱 설치 없이 주소만 열면 된다. 첫 배포 목적은 피드백(Phase 7)이라 가끔 접속이 안 되는 것은 감수한다. 같은 compose 파일을 그대로 VM으로 옮길 수 있어 나중에 바꾸기 쉽다. 오래된 디스크 고장에 대비해 외부 백업이 필수다. 무료 요금제에서 Funnel을 쓸 수 있는지는 설치할 때 확인하고, 안 되면 사설망 방식(10장 A)으로 바꾼다. (2026-10-15) |
| D21 | 노트북 운영체제는 Ubuntu Server 대신 **Linux Mint 22.3 Xfce**(Ubuntu 24.04 LTS 기반)를 쓴다. 노트북: i5-3230M, RAM 4GB, SSD 500GB | 이미 설치를 시작했고, Ubuntu 24.04 기반이라 Docker·Tailscale·rclone을 Ubuntu용 그대로 설치할 수 있다. 화면이 있어 처음 리눅스를 다루기 쉽다. Xfce는 가벼운 데스크톱이라 RAM 4GB에서도 Finora(PostgreSQL + API)를 함께 돌릴 여유가 있다. 데스크톱이 덮개 닫힘을 따로 처리하므로 시스템·Xfce 두 곳 모두 설정한다(`docs/laptop-setup.md`). (2026-10-21) |
| D22 | Phase 8(계좌) 전에 **Phase 7.5 — 사용 피드백 반영(UI/UX 개선)** 을 넣는다. 개선 항목은 하나씩 끝내고 다음으로 넘어간다 | 10월 실제 내역을 입력해 보니 불편한 점이 많았다. Phase 7의 목적이 피드백이었고, 계좌 기능이 붙으면 거래 입력 화면이 더 복잡해지므로 입력 흐름을 먼저 다듬는 편이 다시 고치는 비용이 적다. (2026-10-29) |
| D23 | 카테고리는 **최대 3단계 계층**(대분류 › 중분류 › 소분류)이다. Categories에 `ParentId`(자기 참조)를 두고, 단계는 필수가 아니다(대분류만 있어도 됨). 수입/지출 종류는 대분류를 따른다. 거래는 **어느 단계든** 고를 수 있다. 이름은 대분류끼리(같은 종류 안), 같은 부모 아래끼리 겹칠 수 없다(DB 고유 인덱스, PostgreSQL `NULLS NOT DISTINCT`). 하위 분류는 이미 있는 내 카테고리 아래에만 만들고, 하위 분류가 있으면 삭제할 수 없다. 통계는 대분류 합계(하위 포함) → 눌러서 펼치기, 거래 목록은 가장 아래 이름만 보여준다 | 실제 사용에서 "교통 › 대중교통 › 버스"처럼 나누고 싶다는 요구. 테이블을 단계별로 나누지 않고 자기 참조 하나로 두면 단계를 자유롭게 고를 수 있다. 상위 분류도 고를 수 있게 해서 자세히 나누기 귀찮을 때도 입력이 쉽다. 기존 카테고리·거래는 모두 대분류로 그대로 남는다. (2026-10-29) |
| D24 | **저금**은 가계부 거래와 별개인 `Savings`(저금) + `SavingEntries`(입금·출금·이자 기록)로 만든다. 종류는 **자유저금 / 적금 / 예금** 3가지: 자유저금은 목표 금액·목표일(선택)에 자유롭게 입출금, 적금은 가입일·만기일·월 납입액(정액 / 자유적립 / 주간 증액(26주형))이고 출금 없이 만기 수령·중도해지만, 예금은 원금 입금 1번 + 만기 수령·중도해지만 한다. 연이율·이자 방식(단리 / 월복리)·세율(기본 15.4%, 비과세 0%)로 **예상 이자·세후 수령액**을 Backend `decimal`로 계산하고(원 미만 버림, 화면에 "예상" 표시), 실제 받은 이자는 기록으로 남긴다. **입금·출금은 수입·지출이 아니다**(이미 가진 돈을 옮기는 것) → 통계·가계부 기준 총자산(D17)에 영향 없음. **실제 받은 이자는 새로 생긴 돈**이라 가계부 수입("이자" 기본 카테고리)으로 함께 기록하고(기본 켬) 저금 기록과 연결해 같이 수정·삭제한다. 가계부 요약에 **이번 달 저금(입금 − 출금)** 과 **저금 후 남은 돈(합계 − 저금)** 을 더하고, 대시보드에 저금 카드를 둔다. 금융기관 이름(선택)으로 기관별 합계가 예금자보호 한도(1억 원)를 넘으면 안내한다. **이미 가입해 진행 중인 상품**은 등록할 때 **기존 금액**(정액 적금은 납입 회차 × 월 납입액, 26주형은 납입 주 수로 자동 계산·수정 가능, 자유적립·자유저금은 총액, 예금은 원금)을 기록 1건(`Opening`)으로 넣는다. 기존 금액은 현재 금액·진행률에는 포함하고 **기간별 저금 합계(이번 달 저금)에서는 뺀다**. 자유적립 적금의 기존 금액은 가입일에 넣은 것으로 보고 예상 이자를 근사한다. Phase 8에서 계좌가 생기면 적금·예금을 계좌와 연결할 수 있게 한다 | 실제로 적금·예금을 들고 있어 따로 관리하고 싶다는 요구. 다른 앱 조사 결과 저금은 ① 전용 계좌 + 이체 ② 목표 + 기록 ③ 지출 카테고리 방식이 있었다. ③은 총자산이 틀어지고, ①은 Phase 8(계좌·이체)이 먼저 필요하다. ②로 지금 만들고 나중에 계좌와 연결하면 기록을 다시 입력하지 않아도 된다. 한국 예금·적금은 넣는 방식과 이자 계산(적금은 납입금마다 남은 기간만큼 이자)이 달라서 종류를 나눴다. "합계 + 저금"은 이미 합계에 들어 있는 돈을 두 번 세므로 "합계 − 저금"으로 정했다. 진행 중인 상품의 기존 금액을 오늘 날짜 입금으로 넣으면 이번 달 저금이 부풀고, 가입일 날짜로 넣으면 가계부 기록이 없는 달에 저금만 잡혀서 별도 종류로 뺐다. 잔돈 올림·소득 %·자동이체 같은 자동 규칙은 은행 연동이 필요해서 하지 않는다. (2026-11-05) |
| D25 | 메뉴에 **저금**을 추가한다. 모바일 하단 탭은 6칸을 유지하려고 아직 "준비 중"인 **투자**를 빼고 저금을 넣는다. PC 사이드 메뉴에는 투자도 그대로 둔다 | 하단 탭에 7칸을 넣으면 글자가 좁아진다. 매일 쓰는 기능을 하단에 두고, 투자는 Phase 10에서 만들 때 다시 정한다. (2026-11-05) |
| D26 | 통계 화면은 **위젯 대시보드**로 만든다. 위젯(그래프·표 카드) 종류는 코드에 고정하고(문자열 id), 사용자는 **편집 모드**에서 카드를 드래그해 순서를 바꾸고, 크기(반 칸 / 한 줄)를 고르고, 숨기거나 다시 꺼낼 수 있다. PC는 2열 격자, 모바일은 1열(순서만 적용). 기간 선택은 화면 위 하나를 모든 위젯이 같이 쓴다. 위젯 11개: ① 기간 요약 ② 월별 수입·지출 ③ 카테고리별 지출(펼치기) ④ 일별 지출 추이 ⑤ 이전 기간 누적 비교 ⑥ 카테고리 비중 도넛 ⑦ 상위 카테고리 월별 추이 ⑧ 요일별 평균 지출 ⑨ 큰 지출 TOP 5 ⑩ 저축률 추이 ⑪ 총자산(가계부 기준) 추이. 집계·비율·평균은 모두 Backend `decimal`(D4). 드래그는 **@dnd-kit/core + @dnd-kit/sortable** | 그래프 3개로는 부족하고, 보고 싶은 그래프가 상황마다 달라 사용자가 배치를 정하고 싶다는 요구. 자유 격자(react-grid-layout)는 PC에서 가장 자유롭지만 모바일 터치에서 쓰기 어렵고 설정이 복잡하다. "순서 + 크기 두 가지"만으로도 충분하고 모바일 1열과도 자연스럽다. HTML 기본 드래그는 터치를 지원하지 않는다. dnd-kit은 터치·키보드를 지원하고 코드가 단순하다. 편집 모드에서만 드래그하는 이유: 차트 터치(툴팁)와 겹치지 않고 실수로 옮기지 않는다. (2026-11-11) |
| D27 | 위젯 배치는 **Backend** `DashboardLayouts`(UserId, Page, Layout JSON, UpdatedAt)에 사용자·화면당 1행으로 저장한다. Layout은 `[{ id, size: 'half' \| 'full', hidden }]`을 순서대로 담은 JSON 한 덩어리. 불러올 때 **모르는 위젯 id는 버리고**(옛 배치), **배치에 없는 위젯은 기본 배치 순서로 뒤에 붙인다**(새 위젯이 자동으로 나타남). 바꾸면 잠시 기다렸다가(debounce) 저장하고, "기본 배치로 되돌리기"를 둔다 | 폰과 PC에서 같은 배치가 보여야 하고 사용자 분리 원칙과 맞는다. localStorage는 기기마다 다르고 브라우저 데이터를 지우면 사라진다. 위젯마다 행을 두면 순서를 바꿀 때 여러 행을 고쳐야 하고 조회는 늘 통째로 하므로 JSON 하나로 저장한다. Page 열을 둬서 나중에 대시보드 화면도 같은 방식으로 쓸 수 있다. (2026-11-11) |

---

## 4. 저장소 구조

현재 Vite 프로젝트는 저장소 루트에 있다. **Phase 0에서 `frontend/`로 옮긴다.** (아직 코드가 거의 없어서 지금 옮기는 비용이 가장 작다.)

```text
finora/                      ← Git 저장소 루트
│
├─ CLAUDE.md                 ← Claude Code 작업 규칙
├─ README.md                 ← 프로젝트 소개 / 실행 방법
├─ .gitignore
│
├─ docs/                     ← 설계·계획 문서 (6장 참고)
│
├─ frontend/                 ← React + TypeScript (현재 루트의 Vite 프로젝트)
│  ├─ CLAUDE.md              ← Frontend 전용 규칙 (필요해지면 작성)
│  ├─ src/
│  ├─ public/
│  ├─ package.json
│  └─ vite.config.ts
│
└─ backend/                  ← ASP.NET Core (Phase 3에서 생성)
   ├─ CLAUDE.md              ← Backend 전용 규칙 (필요해지면 작성)
   ├─ Finora.Api/
   └─ Finora.Api.Tests/
```

---

## 5. 전체 기능 구조

```text
Finora
│
├─ 인증
│  ├─ 회원가입 (초대코드)
│  ├─ 로그인
│  └─ 로그아웃
│
├─ 대시보드
│  ├─ 총자산 (가계부 기준 / 계좌 기준 — 결정 D17)
│  ├─ 기간 조회 (일간 / 주간 / 월간 / 연간 / 직접 지정)
│  ├─ 이번 달 수입 / 지출
│  ├─ 투자현황
│  └─ 주요 통계
│
├─ 가계부
│  ├─ 수입
│  ├─ 지출
│  ├─ 이체 (내 계좌 간 이동 — Phase 8, 결정 D16)
│  ├─ 카테고리
│  └─ 월별 조회
│
├─ 자산 (Phase 8, 결정 D16)
│  ├─ 현금
│  ├─ 은행
│  ├─ 예금/적금
│  └─ 기타 자산
│
├─ 저금 (Phase 7.5, 결정 D24 — 모바일 하단 탭은 투자 대신 저금, 결정 D25)
│  ├─ 자유저금 / 적금 / 예금 (진행 중 · 달성 · 보관)
│  ├─ 입금 · 출금 · 이자 기록, 만기 수령 · 중도해지
│  ├─ 진행률 · 월 필요액 · 계획 대비 · 예상 세후 수령액
│  └─ 가계부 요약(이번 달 저금, 저금 후 남은 돈) · 대시보드 저금 카드
│
├─ 투자 (PC 사이드 메뉴만, 결정 D25)
│  ├─ 투자계좌
│  ├─ 보유종목
│  ├─ 매수/매도
│  ├─ 배당
│  ├─ 수익률
│  └─ 평가금액
│
├─ 통계 (위젯 대시보드, 결정 D26·D27)
│  ├─ 기간 조회 (대시보드와 같은 기간 선택) — 모든 위젯이 공유
│  ├─ 위젯: 기간 요약 · 월별 수입/지출 · 카테고리별 지출(펼치기) · 일별 지출 추이 · 이전 기간 누적 비교
│  │        · 카테고리 비중 도넛 · 상위 카테고리 월별 추이 · 요일별 평균 지출 · 큰 지출 TOP 5
│  │        · 저축률 추이 · 총자산(가계부 기준) 추이
│  ├─ 편집 모드: 드래그로 순서, 크기(반 칸 / 한 줄), 숨기기 — 배치는 사용자별 저장
│  ├─ 자산 변화 (계좌 기준, Phase 8)
│  └─ 투자 성과 (Phase 10)
│
└─ 설정
   ├─ 사용자 정보 / 비밀번호 변경
   ├─ 카테고리 관리
   └─ 환경설정
```

---

## 6. Frontend 프로젝트 구조

```text
frontend/src/
│
├─ assets/                   이미지, 아이콘
│
├─ components/               여러 화면에서 재사용하는 UI
│  ├─ ui/                    shadcn/ui가 생성한 기본 컴포넌트 (Button, Input, Dialog ...)
│  ├─ common/                Finora 공통 컴포넌트 (MoneyText ...)
│  ├─ statistics/            통계 위젯 (그래프 카드 하나 = 파일 하나) + 위젯 목록·배치 틀
│  ├─ transaction/
│  ├─ asset/
│  └─ investment/
│
├─ layouts/                  화면 공통 골격
│  ├─ MainLayout.tsx         사이드 메뉴 + 상단바 + 본문
│  └─ AuthLayout.tsx         로그인/가입 화면용
│
├─ pages/                    실제 화면 (라우트 1개 = 페이지 1개)
│  ├─ LoginPage.tsx
│  ├─ RegisterPage.tsx
│  ├─ DashboardPage.tsx
│  ├─ TransactionsPage.tsx
│  ├─ AssetsPage.tsx
│  ├─ InvestmentsPage.tsx
│  ├─ StatisticsPage.tsx
│  ├─ SettingsPage.tsx
│  └─ NotFoundPage.tsx       잘못된 주소
│
├─ services/                 Backend 통신 (Phase 2에서는 임시 데이터 반환)
│  ├─ api.ts                 fetch 공통 처리 (에러, 401 처리)
│  ├─ authService.ts
│  ├─ transactionService.ts
│  ├─ assetService.ts
│  └─ investmentService.ts
│
├─ mocks/                    Phase 2용 임시 데이터 (Phase 5 이후 삭제)
│
├─ types/                    TypeScript 데이터 구조
│  ├─ user.ts
│  ├─ transaction.ts
│  ├─ asset.ts
│  └─ investment.ts
│
├─ hooks/                    재사용 React 로직
├─ lib/                      cn() 등 shadcn 유틸
├─ utils/                    포맷 함수 (금액, 날짜) 등
│
├─ router.tsx                라우트 정의 (주소 ↔ 화면 연결표)
├─ main.tsx                  시작점. RouterProvider 렌더링
└─ index.css
```

> v1에서는 `components/layout/`과 `layouts/`가 중복되어 있었다. `layouts/` 하나로 통일한다.

### 폴더 규칙

| 폴더 | 역할 | 규칙 |
|---|---|---|
| pages | 실제 화면 | 데이터는 services(또는 hooks)를 통해서만 가져온다 |
| components | 재사용 UI | 직접 API를 호출하지 않는다. props로 받는다. 예외: `statistics/` 위젯은 각자 `useQuery`로 service를 부른다 (숨긴 위젯은 불러오지 않게, 결정 D26) |
| layouts | 공통 골격 | |
| services | Backend 통신 | 화면에서 `fetch`를 직접 쓰지 않는다 |
| mocks | 임시 데이터 | services만 import한다 |
| types | 데이터 구조 | Backend DTO와 이름·형태를 맞춘다 |
| utils | 공통 함수 | 금액은 `Intl.NumberFormat('ko-KR')`로 표시 |

---

## 7. Backend 프로젝트 구조

```text
backend/
├─ Finora.Api/
│  ├─ Controllers/           HTTP 요청/응답만 담당
│  ├─ DTOs/                  요청/응답 데이터 형태
│  ├─ Services/              비즈니스 로직 (수익률 계산 등)
│  ├─ Data/
│  │  ├─ FinoraDbContext.cs
│  │  ├─ Entities/           DB 테이블 클래스
│  │  └─ Migrations/         EF Core 마이그레이션
│  ├─ Auth/                  현재 사용자 확인, 초대코드
│  ├─ Middleware/            공통 오류 처리
│  └─ Program.cs
│
└─ Finora.Api.Tests/         xUnit 테스트 (특히 사용자 분리 테스트)
```

### 요청 흐름

```text
React
  │  HTTP 요청 (Cookie 자동 포함)
  ▼
Controller   ← [Authorize], 입력 검증
  │
  ▼
Service      ← 계산, 규칙
  │
  ▼
DbContext    ← 현재 UserId 필터 자동 적용
  │
  ▼
PostgreSQL
```

- Frontend는 DB에 직접 접근하지 않는다.
- Controller는 Entity를 그대로 반환하지 않고 DTO로 변환해 반환한다. (PasswordHash 같은 값이 실수로 나가지 않도록)

---

## 8. Database 설계

```text
Users ──────┬── Categories
            │
            ├── Accounts (자산: 현금/은행/예금/적금/기타)
            │     ├── AccountSnapshots (월말 잔액 기록)
            │     └── Transactions (수입/지출/이체) ── Categories
            │
            ├── Savings (저금: 자유저금/적금/예금, 결정 D24)
            │     └── SavingEntries (입금/출금/이자) ── Transactions (이자 수입만 연결)
            │
            └── InvestmentAccounts
                  └── InvestmentTransactions ── Securities (공용)

InviteCodes   (공용, 관리자 관리)
ExchangeRates (공용, 외부 API 캐시)
```

### 공통 규칙

| 항목 | 규칙 |
|---|---|
| 금액 | C# `decimal`, PostgreSQL `numeric(18,2)`. 주식 수량·단가는 `numeric(18,6)`. **float/double 금지** |
| 날짜 | 거래일은 `date` (한국 기준 날짜). 생성/수정 시각은 `timestamptz` (UTC 저장) |
| 소유권 | 사용자 소유 테이블에는 모두 `UserId`를 둔다 |
| 공통 컬럼 | `CreatedAt`, `UpdatedAt` |

### 테이블

**Users** — ASP.NET Core Identity가 생성 (Id, UserName, PasswordHash, 잠금 정보 등)

**InviteCodes**
- Id, Code, CreatedAt, UsedAt, UsedByUserId

**Categories**
- Id, UserId, ParentId(상위 카테고리, 대분류는 null — 최대 3단계, 결정 D23), Name, Type(Income/Expense), SortOrder
- 가입 시 기본 카테고리(식비, 교통, 주거 …)를 자동 생성

**Accounts** (자산)
- Id, UserId, Name, Type(Cash/Bank/Deposit/Savings/Other), Currency, Balance, IsActive

**AccountSnapshots** — "자산 변화" 그래프를 그리려면 과거 값이 필요하다
- Id, UserId, AccountId, Date, Balance

**Transactions** (가계부)
- Id, UserId, Date, Type(Income/Expense/Transfer)
- AccountId — 어느 계좌에서 나갔는지/들어왔는지
- ToAccountId — 이체일 때만
- CategoryId — 이체일 때는 없음
- Amount (항상 양수), Memo

> 이체(Transfer)가 없으면 "월급통장 → 적금" 이동이 지출로 잡혀 통계가 틀어진다.

**Savings** (저금, 결정 D24)
- Id, UserId, Name, Kind(Free/Installment/Deposit), BankName(선택)
- TargetAmount(자유저금 목표 금액), StartDate, MaturityDate(자유저금은 목표일·선택, 적금·예금은 만기일·필수)
- InterestRate(연 %, `numeric(6,3)`), InterestMethod(Simple/MonthlyCompound), TaxRate(%, `numeric(5,2)`, 기본 15.4)
- PaymentType(적금만: Fixed/Flexible/WeeklyIncreasing), PlannedAmount(월 납입액·26주 첫 주 금액·자유저금 월 계획 금액)
- Status(Active/Completed/Archived), Memo

**SavingEntries** (저금 기록)
- Id, UserId, SavingId, Date, Type(Opening/Deposit/Withdrawal/Interest), Amount(항상 양수), Memo
- Opening(기존 금액) — 이미 가입한 상품을 등록할 때 지금까지 넣은 돈. 저금 하나에 1건만, 기간별 저금 합계에서는 뺀다
- TransactionId — 이자를 가계부 수입으로 함께 기록했을 때 그 거래 (같이 수정·삭제)

> 입금·출금은 가계부 거래가 아니다. 이자만 가계부 수입과 연결한다. (결정 D24)

**DashboardLayouts** (통계 위젯 배치, 결정 D27)
- Id, UserId, Page(`statistics` — 나중에 대시보드 화면도 같은 방식), Layout(`jsonb`: `[{ id, size, hidden }]` 순서대로), UpdatedAt
- 사용자·화면당 1행 (UserId + Page 고유 인덱스). 행이 없으면 기본 배치

**InvestmentAccounts**
- Id, UserId, Name, Broker, Currency

**Securities** (종목 — 사용자 공용 기준정보)
- Id, Symbol, Market(KRX/NASDAQ …), Name, Currency

**InvestmentTransactions**
- Id, UserId, InvestmentAccountId, SecurityId
- Type(Buy/Sell/Dividend)
- Quantity, Price, Fee, Tax, Currency
- ExchangeRate — 외화 종목일 때 거래 시점 환율
- Date, Memo

> **보유수량·평균매수가·손익은 테이블에 저장하지 않고 거래내역으로 계산한다.** 저장하면 거래 수정/삭제 시 값이 어긋난다. 평균매수가는 이동평균법(국내 증권사 표시 방식)을 기본으로 한다.

**ExchangeRates**
- Id, Date, Currency, Rate

---

## 9. 보안 원칙

모든 사용자 데이터는 로그인한 사용자 기준으로 접근한다.

```text
사용자 A 로그인
      ↓
Cookie로 인증 정보 확인 (Backend)
      ↓
인증 정보에서 현재 UserId 확인
      ↓
UserId = A인 데이터만 조회/수정/삭제
```

사용자가 URL이나 요청값으로 다른 UserId나 다른 사람의 데이터 Id를 보내더라도 접근할 수 없어야 한다.

### 원칙

**인증**
- 비밀번호는 Identity의 Hash로 저장 (평문 저장 금지)
- Cookie는 `HttpOnly`, `Secure`, `SameSite=Strict`
- 로그인 실패 반복 시 계정 잠금 (Identity Lockout)
- 로그인 API에 요청 횟수 제한 (ASP.NET Core Rate Limiting). 비밀번호 변경 API도 같은 제한 (현재 비밀번호 반복 추측 방지)
- 비밀번호를 바꾸면 다른 기기의 로그인은 끊긴다 (SecurityStamp, 1분마다 확인). 바꾼 기기는 유지

**데이터 분리**
- UserId는 **항상 인증 정보에서** 가져온다. 요청 Body/URL의 UserId는 무시한다
- EF Core **Global Query Filter**로 모든 조회에 `UserId = 현재 사용자` 조건을 자동 적용한다
- 수정/삭제 전에도 소유권을 확인한다
- 다른 사람의 데이터 Id로 요청하면 `403`이 아니라 `404`를 반환한다 (존재 여부도 숨김)
- 사용자 분리는 **자동 테스트로 검증**한다 (A가 B의 데이터 조회/수정/삭제 시도 → 모두 실패)

**운영**
- 비밀번호, DB 접속 문자열, API Key는 Git에 올리지 않는다 (개발: `dotnet user-secrets`, 운영: 환경변수). 필요한 설정은 `.env.example`처럼 값 없는 예시로만 남긴다
- 저장소가 **Public**이므로, 비밀이 한 번이라도 push되면 커밋 삭제가 아니라 **즉시 키/비밀번호 교체**로 대응한다
- 테스트 데이터·스크린샷은 가짜 데이터만 사용한다. DB 백업 파일, 서버 IP, 초대코드는 저장소에 넣지 않는다
- HTTPS 필수 (PWA 설치에도 필요)
- DB는 매일 백업하고, 복원이 실제로 되는지 확인한다

---

## 10. 배포 / 운영

가족·지인이 집 밖에서 Android로 접속하려면 서버가 인터넷에서 접근 가능해야 한다. **Phase 7(첫 배포) 전까지** 아래 중 하나를 결정한다.

| 방식 | 장점 | 단점 |
|---|---|---|
| A. 집 PC/미니PC + Tailscale | 인터넷에 공개되지 않아 가장 안전. 비용 거의 없음 | 사용자마다 Tailscale 앱 설치 필요. 집 PC가 꺼지면 사용 불가 |
| B. 클라우드 VM (NCP, AWS Lightsail 등) + 도메인 | 어디서나 접속, 설치 불필요 | 월 비용. 서버 보안 관리 책임 |
| C. PaaS (Azure App Service 등) + 관리형 PostgreSQL | 서버 관리 부담 적음 | 비용이 상대적으로 높음 |
| **A'. 집 노트북 + Tailscale Funnel (선택, 결정 D20)** | 비용 거의 없음. 앱 설치 없이 `*.ts.net` 주소로 접속. 공유기 포트를 열지 않음 | 정전·집 인터넷 장애·노트북 고장 때 사용 불가. 주소 형식이 정해져 있음. Tailscale 서비스에 의존 |

공통 필요 사항: HTTPS 인증서, DB 백업(외부 저장소로 복사), 장애 시 로그 확인 방법.

```text
[Android/PC 브라우저]
      │ HTTPS  https://finora.<tailnet>.ts.net
      ▼
 Tailscale Funnel (HTTPS 처리, 집 IP 숨김)
      │
      ▼
┌──────── 집 노트북 (Ubuntu Server) ────────┐
│ Docker Compose                            │
│   ├ ASP.NET Core (/api + React 빌드 파일)  │
│   └ PostgreSQL                            │
│ 매일 DB 백업 → 노트북 밖 저장소로 복사       │
└───────────────────────────────────────────┘
```

---

## 11. 개발 단계

각 단계는 **완료 기준**을 모두 만족해야 다음으로 넘어간다.

### Phase 0 — 저장소 정리 ✅ 2026-10-07 완료

작업:
1. `git init`, 첫 커밋
2. Vite 프로젝트를 `frontend/`로 이동
3. `docs/`, `CLAUDE.md` 정리 (완료)
4. `README.md`를 Finora 소개로 교체

완료 기준:
- `frontend/`에서 `npm run dev`, `npm run lint`, `npm run build` 성공
- Git에 첫 커밋 존재

---

### Phase 1 — Frontend 기본 구조 ✅ 2026-10-07 완료

목표:
- React 기본 구조 이해 (컴포넌트, props, state)
- TypeScript 기본 문법 익히기

작업:
1. Vite 기본 코드 정리 (App.tsx, App.css, index.css, 샘플 이미지)
2. `src` 폴더 구조 생성
3. React Router 설치 및 페이지 라우팅
4. MainLayout (사이드 메뉴 — 모바일에서는 하단/햄버거 메뉴)

완료 기준:
- 메뉴를 눌러 8개 페이지(빈 화면)로 이동 가능
- 브라우저 폭을 줄이면 모바일 레이아웃으로 바뀜

---

### Phase 2 — Finora 화면 제작 (임시 데이터) ✅ 2026-10-12 완료

Backend 없이 화면부터 만든다. 임시 데이터는 `mocks/`에 두고 **services를 통해서만** 사용한다. (결정 D5)
첫 배포 범위(가계부 중심)의 화면만 만든다. 투자는 메뉴만 남기고 "준비 중"으로 표시한다. (결정 D10)

순서:

```text
Login → Dashboard → Transactions → Assets(계좌) → Statistics → Settings → Investments('준비 중')
```

목표:
- 화면 구조 확정
- 공통 컴포넌트 (Button, Input, Modal, MoneyText)
- 처음부터 반응형 (PC / 모바일)
- `types/`에 데이터 구조 정의 → 8장 DB 설계와 맞추기

작업:
- Dashboard: 총자산, 이번 달 수입/지출, 최근 거래 (투자 카드 제거)
- Transactions: 수입/지출/이체 등록·수정·삭제
- Transactions 보기 방식 (결정 D11): 기간 요약(수입/지출/합계) + 탭
  - 목록: 날짜별 묶음 + 날짜마다 소계
  - 달력: 칸마다 그날 수입/지출, 날짜를 누르면 그날 거래
  - 월별: `< 2026년 >` + 12개월 합계 표, 행을 누르면 그 달 목록으로 이동 (연간 보기 겸용)
- Assets: 계좌 목록, 계좌별 잔액, 합계
- Statistics: 월별 수입/지출, 카테고리별 지출 비중 (Recharts 도입)
- Settings: 카테고리 관리, 비밀번호 변경 화면
- Investments: "준비 중" 안내

완료 기준:
- 위 화면이 모두 임시 데이터로 표시됨
- 가계부 화면에서 등록/수정/삭제가 화면상으로 동작 (새로고침하면 초기화되어도 됨)

---

### Phase 3 — Backend + Database 기반 ✅ 2026-10-13 완료

v1의 Phase 3(Backend)과 Phase 4(DB)를 합쳤다. DB 없는 API는 확인할 것이 거의 없기 때문이다.

작업:
1. PostgreSQL 실행 — Docker (`docker compose up -d`, 결정 D12)
2. `backend/`에 ASP.NET Core Web API 프로젝트 생성
3. EF Core + Npgsql 연결, 첫 Migration
4. 공통 오류 처리 Middleware
5. Vite proxy 설정 (`/api` → Backend)
6. 테스트 프로젝트 생성

완료 기준:
- `GET /api/health`가 DB 연결 상태를 반환
- Frontend에서 해당 API 호출 결과를 화면에 표시

---

### Phase 4 — 회원가입 / 로그인 / 사용자 분리 ✅ 2026-10-13 완료

**실제 금융 데이터를 저장하기 전에** 완성한다. (결정 D6)

작업:
1. Identity 테이블 (`FinoraUser`, `IdentityDbContext`), InviteCodes ↔ Users 연결
2. Cookie 인증 설정 (HttpOnly·Secure·SameSite=Strict, API는 401 응답, 5회 실패 잠금)
3. 초대코드 발급 명령어 (결정 D14) + 초대코드 회원가입 API (`POST /api/auth/register`), Testcontainers 테스트 기반 (결정 D13)
4. 로그인 / 로그아웃 / 로그인 유지, 현재 사용자 확인 (`GET /api/me`), 로그인 요청 횟수 제한
5. 사용자 분리 기반: 현재 사용자 서비스, Global Query Filter, Categories 테이블·가입 시 기본 카테고리·조회 API (결정 D15), 사용자 분리 자동 테스트
6. Frontend: 로그인·가입 화면 연결, 로그인 안 된 상태에서 페이지 접근 시 로그인 화면으로 이동

완료 기준:
- 초대코드 없이는 가입 불가
- 로그인 5회 실패 시 잠금
- 사용자 분리 자동 테스트 통과

---

### Phase 5 — 가계부 완성 (첫 실제 기능) ✅ 2026-10-14 완료

Frontend ↔ Backend를 처음으로 실제로 연결하는 단계. 가계부 하나를 끝까지 완성하며 연결 방식을 익힌다.
계좌와 이체는 Phase 8로 미룬다. 화면 코드는 남겨 두고 숨기거나 "준비 중"으로 보여준다. (결정 D16)

API 예 (응답 형태는 Phase 2의 `frontend/src/types/`와 services 주석에 맞춘다):

```text
GET    /api/transactions/monthly?year=2026&month=10   월 요약 + 날짜별 묶음·소계 (목록·달력 탭)
GET    /api/transactions/yearly?year=2026             한 해 요약 + 12개월 합계 (월별 탭)
POST   /api/transactions        수입/지출만. 계좌는 선택 항목(아직 없음), 이체는 거절
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
GET    /api/categories          (POST / PUT / DELETE: 같은 종류 안 이름 중복 금지, 사용 중이면 삭제 불가)
```

기능:
- 수입 / 지출 등록, 수정, 삭제
- 카테고리 관리 (가입 시 기본 카테고리 자동 생성)
- 월별 조회(목록·달력), 연간 월별 합계

작업:
1. 카테고리 관리 API + 설정 화면 연결, TanStack Query 도입
2. 거래 저장: Transactions 테이블, 등록·수정·삭제 API, 카테고리 소유권 확인, 사용자 분리 테스트
3. 거래 조회: 월별(날짜별 묶음·소계)·연간 집계 API, 합계 자동 테스트
4. 가계부 화면 연결 (`transactionService` mock → 실제 API). 계좌 선택·이체 숨김, `accountId` 선택 항목으로, 자산 메뉴 "준비 중"
5. 마무리: 로그인 만료(401) 시 로그인 화면으로 이동, 완료 기준 확인

완료 기준:
- 새로고침/재로그인 후에도 데이터 유지
- 다른 사용자로 로그인하면 내 데이터가 보이지 않음
- 월별·연간 합계가 거래 내역과 일치 (자동 테스트)

---

### Phase 6 — 대시보드 / 통계 / 설정 ✅ 2026-10-14 완료

첫 배포에서 가족이 "가계부 + 한눈에 보기"를 쓸 수 있도록, 집계 화면을 실제 데이터로 연결한다.

기능:
- 대시보드: 총자산 2종류(가계부 기준 표시, 계좌 기준은 "준비 중"), 이번 달 수입/지출, 최근 거래 (결정 D17)
- 기간 조회: 일간 / 주간(월~일) / 월간 / 연간 / 달력으로 직접 지정. 기간의 수입·지출·합계 + 기간 마지막 날 기준 누적 총자산. 대시보드와 통계에서 같은 기간 선택을 쓴다
- 통계: 월별 수입/지출 그래프, 카테고리별 지출 비중, 월별 비교 (선택한 기간 기준)
- 설정: 비밀번호 변경

API 예:

```text
GET    /api/dashboard                                  최근 거래 (기간별 숫자는 아래 summary)
GET    /api/statistics/summary?from=2026-10-01&to=2026-10-31   기간 수입·지출·합계 + to 기준 누적 총자산
```

규칙:
- 합계·비중 계산은 Backend `decimal`로 한다. Frontend는 받은 값을 그리기만 한다 (결정 D4)

완료 기준:
- 대시보드·통계 숫자가 가계부 데이터와 일치 (집계 로직 자동 테스트)

---

### Phase 7 — 첫 배포 ✅ 2026-10-29 완료

가계부 중심 기능만 있는 상태로 **가족이 실제로 써보게** 한다. 남은 기능을 만들기 전에 피드백을 받는 것이 목적이다.

작업 (배포 방식은 결정 D20: 집 노트북 + Tailscale Funnel):
- [x] 10장 배포 방식 결정 → 결정 기록(3장) D19 → D20
- [x] 7-1 React 빌드 파일(wwwroot) 제공: 기본값이 "로그인 필요"(fallback policy)라서 `UseStaticFiles`는 `UseAuthorization`보다 앞에 두고, SPA fallback(`MapFallbackToFile`)에는 `AllowAnonymous`를 붙인다. 안 하면 로그인 화면 자체가 401이 된다
- [x] 7-2 프록시(Tailscale) 뒤 Forwarded Headers 설정: 안 하면 로그인 요청 횟수 제한이 모든 사용자를 프록시 IP 하나로 묶어 센다
- [x] 7-3 운영용 Dockerfile + docker-compose + 운영 환경변수 예시 (DB Migration 적용 방법 포함)
- [x] 7-4 DB 백업 스크립트(노트북 밖 암호화 복사) + 복원 테스트 스크립트 (`scripts/`, 사용법은 `docs/operations.md`). 매일 자동 실행·rclone 설정은 7-5
- [x] 7-5 노트북 준비 (`docs/laptop-setup.md`): Linux Mint 설치(결정 D21), 덮개 닫아도 안 꺼지게, 자동 보안 업데이트, Docker, Tailscale Funnel로 HTTPS 공개, rclone(crypt → Google Drive) 설정 + 백업 매일 자동 실행(systemd user timer, `scripts/systemd/`). 클라우드에서 내려받은 백업으로 복원 테스트 성공

완료 기준:
- Android 폰에서 외부 네트워크로 접속·로그인 가능
- 백업 파일로 다른 DB에 복원 성공

---

### Phase 7.5 — 사용 피드백 반영 (UI/UX 개선)

10월 실제 내역을 입력해 보고 나온 불편한 점을 고친다. 항목은 하나씩 끝내고(만들기 → 확인 → 커밋 → 운영 배포) 다음으로 넘어간다. (결정 D22)

1. 카테고리 3단계 계층 (결정 D23)
   - [x] Backend: `ParentId` + Migration, 이름 중복 규칙(대분류끼리·같은 부모 아래끼리)
   - [x] Backend API: 하위 분류 추가(최대 3단계, 내 카테고리 아래에만), 하위 분류가 있으면 삭제 불가, 자동 테스트
   - [x] 설정 화면: 카테고리 트리, 하위 분류 추가
   - [x] 거래 등록·수정: 계층에서 고르기 (선택하면 전체 경로 표시), 목록은 가장 아래 이름
   - [x] 통계: 대분류 합계(하위 포함) → 눌러서 펼치기 (Backend decimal)
   - [x] 운영 배포·확인 (2026-10-30, Migration AddCategoryParent)
   - ✅ 2026-10-30 완료
2. 저금 — 자유저금 / 적금 / 예금 (결정 D24, D25) — **2026-11-11 잠시 멈춤**: ③ 통계 대시보드를 먼저 하고 Backend API부터 이어서 한다
   - [x] 계획서 반영 (D24, D25, 8장 Savings·SavingEntries, 메뉴 구조)
   - [x] Backend: `Savings`·`SavingEntries` + Migration, "이자" 기본 수입 카테고리(기존 사용자 포함)
   - [ ] Backend API: 저금 등록·수정·삭제·상태 변경, 이미 가입한 상품의 기존 금액(Opening 1건), 기록 입력(종류별 규칙), 만기 수령·중도해지, 이자 → 가계부 수입 연결, 사용자 분리 테스트
   - [ ] Backend 계산: 예상 이자·세금·세후 수령액(단리·월복리·적금 회차별, 기존 금액 반영), 진행률, 월 필요액, 계획 대비(앞섬/뒤처짐), 기간별 저금 합계(기존 금액 제외) (decimal, 단위 테스트)
   - [ ] 메뉴: 모바일 하단 탭 투자 → 저금, PC 사이드 메뉴에 저금 추가
   - [ ] 저금 화면: 목록(진행 중·달성·보관, 전체 합계), 등록·수정 폼(종류별 칸, 새로 가입 / 이미 가입한 상품 + 기존 금액)
   - [ ] 저금 상세: 기록 입력, 만기 수령·중도해지, 누적 그래프, 26주 계획표, 예금자보호 안내
   - [ ] 가계부 요약(이번 달 저금, 저금 후 남은 돈) + 대시보드 저금 카드
   - [ ] 전체 테스트(dotnet test, lint, build, 브라우저) + 운영 배포·확인
3. 통계 대시보드 — 위젯 11개, 드래그로 배치, 사용자별 저장 (결정 D26, D27)
   - [x] 계획서 반영 (D26, D27, 8장 DashboardLayouts, 5장 통계 메뉴, 6장 `components/statistics/`)
   - [x] 위젯 틀: 기존 카드 3개를 `components/statistics/` 위젯으로 분리, 위젯 목록(id·이름·기본 크기), 기본 배치, PC 2열 격자(반 칸 / 한 줄)·모바일 1열. 아직 드래그 없음
   - [x] 드래그 앤 드롭: `@dnd-kit/core` + `@dnd-kit/sortable` 설치, 편집 모드 켜기/끄기, 손잡이로 드래그(터치·키보드), 크기 바꾸기, 숨기기·숨긴 위젯 다시 꺼내기. 배치는 아직 메모리에만
   - [ ] Backend: `DashboardLayouts` + Migration, `GET/PUT /api/dashboard-layouts/statistics`, 검증(위젯 id·크기 값, 중복 id), 사용자 분리 테스트
   - [ ] 배치 저장 연결: 불러오기(없으면 기본 배치, 모르는 id 버림·새 위젯 뒤에 추가), 바꾸면 debounce 저장, 기본 배치로 되돌리기
   - [ ] 위젯 ④ 일별 지출 추이 + ⑤ 이전 기간 누적 비교: `GET /api/statistics/daily?from&to` (거래 없는 날 0, 최대 366일). ⑤는 이번 기간·이전 기간 두 번 불러 누적선 2개
   - [ ] 위젯 ⑥ 카테고리 비중 도넛(기존 categories API) + ⑦ 상위 카테고리 월별 추이: `GET /api/statistics/categories/monthly?from&to&top=5` (대분류 기준, 나머지는 "기타"). 차트 색 토큰(`--chart-1~5`)에 구분되는 색 지정
   - [ ] 위젯 ⑧ 요일별 평균 지출: `GET /api/statistics/weekdays?from&to` (요일별 합계 ÷ 기간 안 그 요일 날 수, 원 미만 반올림) + ⑨ 큰 지출 TOP 5: `GET /api/statistics/top-expenses?from&to&count=5`
   - [ ] 위젯 ⑩ 저축률 추이 + ⑪ 총자산(가계부 기준) 추이: 월별 API 응답에 `savingRate`(%, (수입−지출)÷수입, 수입 0이면 null)·`totalAssets`(그달 말 누적) 추가
   - [ ] 전체 테스트(dotnet test, lint, build, 브라우저: PC·폰 드래그) + 운영 배포·확인 (Migration AddDashboardLayouts)

---

### Phase 8 — 자산(계좌) 관리

Phase 5에서 미룬 계좌와 이체를 만든다. (결정 D16) Phase 2 화면 코드(계좌 선택, 이체, 자산 화면)를 다시 보이게 한다.

기능:
- 계좌 등록 / 수정 / 비활성화 (현금 / 은행 / 예금 / 적금 / 기타)
- 거래에 계좌 연결 (`AccountId` 선택 컬럼 추가. 기존 거래는 "계좌 미지정"), 이체 (내 계좌 간 이동)
- 계좌 잔액과 계좌 기준 총자산 (결정 D17). 시작 전에 잔액 관리 방식(저장 vs 거래로 계산)을 결정 기록에 남긴다
- 월말 잔액 스냅샷 (자산 변화 기록), 통계에 자산 변화 그래프 추가

완료 기준:
- 거래를 등록·수정·삭제하면 계좌 잔액이 맞게 바뀜 (자동 테스트)

---

### Phase 9 — PWA / Android 대응

반응형은 Phase 1부터 적용되어 있으므로, 이 단계는 **설치 가능한 앱**으로 만드는 작업이다.

1차 목표:
- vite-plugin-pwa (manifest, 아이콘, service worker)
- Android 홈 화면 설치, Windows 앱 설치

필요성이 확인된 후:
- React Native 등 별도 Android 앱 검토

처음부터 Android 전용 앱을 따로 만들지 않는다.

---

### Phase 10 — 투자 관리

기능:
- 투자계좌, 종목 등록
- 매수 / 매도 / 배당 (수수료·세금 포함)
- 보유수량, 평균매수가 (거래내역으로 계산)
- 실현 / 미실현 손익, 수익률
- 외화 종목 (거래 시점 환율 저장)
- 대시보드 투자 카드, 통계 투자 성과·자산 배분

완료 기준:
- 계산 로직 단위 테스트 통과 (매수 → 일부 매도 → 추가 매수 시나리오)
- 증권사 앱에 표시된 평균매수가와 일치

---

### Phase 11 — 외부 금융 API

```text
주식/환율 API → ASP.NET Core (캐시) → Finora
```

기능:
- 현재 주가, 환율, 종목 정보
- 평가금액 계산

규칙:
- API Key는 Backend에만 둔다
- 요청마다 외부 API를 부르지 않는다. 가격은 DB/메모리에 캐시하고 일정 주기로 갱신한다
- 후보: 공공데이터포털 주식시세(일별), 한국수출입은행 환율 API, 증권사 Open API(실시간) — 착수 시 이용 조건 확인 후 결정

---

### Phase 12 — AI 기능

마지막 단계에서 추가한다.

```text
Finora 데이터
     ↓
AI 분석 (Backend에서 호출)
     ↓
지출 패턴 분석
자산 변화 분석
투자 현황 분석
월별 금융 요약
```

- 핵심 금융 데이터 구조가 안정된 이후 추가한다.
- 외부 AI 서비스로 금융 데이터를 보내므로, **사용자별 동의**를 받고 필요한 최소 데이터(집계값 위주)만 보낸다.

---

### 검토 후보 (우선순위 미정)

- 은행/카드 엑셀·CSV 가져오기 — 가계부 입력 부담을 크게 줄여준다
- 반복 거래 (월세, 구독료 자동 등록)
- 예산 설정 및 초과 알림
- 데이터 내보내기 (사용자 본인 데이터 전체 다운로드)

---

## 12. 최종 완성 형태

```text
                    FINORA
                       │
       ┌───────────────┼───────────────┐
       │               │               │
      인증            금융관리          투자
       │               │               │
    회원가입         가계부            주식
    로그인           자산              배당
    로그아웃         통계              수익률
       │               │               │
       └───────────────┼───────────────┘
                       │
                 ASP.NET Core
                       │
           ┌───────────┴───────────┐
       PostgreSQL           외부 금융 데이터
```

```text
┌────────────────────────────────────────┐
│ FINORA                    사용자 ▼      │
├──────────┬─────────────────────────────┤
│          │                             │
│ 대시보드 │        Dashboard            │
│          │                             │
│ 가계부   │  총자산     ₩52,300,000     │
│          │  수입       ₩3,500,000      │
│ 자산     │  지출       ₩1,250,000      │
│          │  투자수익률     +12.4%       │
│ 투자     │                             │
│          │  [자산 그래프]              │
│ 통계     │  [지출 그래프]              │
│          │                             │
│ 설정     │                             │
└──────────┴─────────────────────────────┘
```

---

## 13. 진행 현황

단계가 끝나면 여기를 갱신한다.

```text
[완료] 개발 환경    Node.js, npm, Git, VS Code
[완료] Vite + React + TypeScript + ESLint 프로젝트 생성 및 실행
[완료] Phase 0   저장소 정리 (Git: jang95 계정, local 설정)
[완료] Phase 1   Frontend 기본 구조 (React Router, PC 사이드 메뉴 / 모바일 하단 탭)
[완료] Phase 2   화면 제작 (임시 데이터) — 로그인/가입, 대시보드(최근 거래), 가계부(등록·수정·삭제·이체, 목록·달력·월별 탭), 자산, 통계(Recharts), 설정(카테고리·비밀번호), 투자 "준비 중"
[완료] Phase 3   Backend + DB 기반 — PostgreSQL(Docker), ASP.NET Core API, EF Core 첫 Migration(InviteCodes), 공통 오류 처리(ProblemDetails), Vite proxy, 설정 화면 서버 상태, xUnit 테스트
[완료] Phase 4   회원가입 / 로그인 / 사용자 분리 — Identity + Cookie 인증(5회 실패 잠금, 로그인 요청 횟수 제한), 초대코드 발급 명령어·가입 API, 로그인 유지, GET /api/me, Global Query Filter, Categories(가입 시 기본 카테고리, 조회 API), Testcontainers 사용자 분리 테스트, 로그인·가입 화면 연결, 로그인 안 하면 로그인 화면으로 이동
[완료] Phase 5   가계부 완성 — 카테고리 추가·이름 변경·삭제(사용 중이면 삭제 불가), 거래 등록·수정·삭제(수입/지출, 카테고리 소유권 확인), 월별·연간 조회(Backend decimal 집계), TanStack Query, 계좌·이체 숨김·자산 "준비 중"(결정 D16), 로그인 만료 시 로그인 화면 이동. 계좌·이체는 Phase 8
[완료] Phase 6   대시보드 / 통계 / 설정 — 기간 선택(일·주·월·연·직접 지정, PeriodPicker), 기간 요약 API(수입·지출·합계 + 기간 말 누적 총자산), 대시보드(기간 카드·계좌 기준 "준비 중"·최근 거래), 통계(카테고리별 지출 비중·월별 그래프, Backend decimal), 비밀번호 변경(다른 기기 로그아웃), 한국 시간 기준(결정 D18)
[완료] Phase 7   첫 배포 — React 빌드 제공, Forwarded Headers, 운영 Dockerfile·compose, 집 노트북(Linux Mint) + Tailscale Funnel HTTPS 공개, DB 백업(rclone crypt → Google Drive, 매일 자동) + 클라우드 백업 복원 테스트, Android 외부 접속 확인
[진행] Phase 7.5 사용 피드백 반영 (UI/UX 개선) — ① 카테고리 3단계 계층 완료 (결정 D22, D23), ② 저금(자유저금·적금·예금) Backend 테이블까지 만들고 잠시 멈춤 (결정 D24, D25), ③ 통계 대시보드(위젯 11개·드래그 배치·사용자별 저장) 진행 중 (결정 D26, D27)
[ ]    Phase 8   자산(계좌) 관리 (계좌·이체·계좌 기준 총자산, 월말 스냅샷)
[ ]    Phase 9   PWA / Android
[ ]    Phase 10  투자
[ ]    Phase 11  외부 금융 API (주가·환율)
[ ]    Phase 12  AI
```

---

## 14. 개발 원칙

**처음부터 완성품을 만들려고 하지 않는다.**

작게 만들고 → 실행하고 → 확인하고 → 이해하고 → 커밋하고 → 다음 단계로 넘어간다.

- 사용자가 React/TypeScript를 아직 깊게 익히지 않은 상태이므로, 각 단계에서 필요한 개념을 실제 Finora 코드로 배우면서 진행한다.
- 한 번에 하나의 기능만 만든다. 동작을 확인하면 Git에 커밋한다.
- 라이브러리는 필요한 순간에 하나씩 추가하고, 왜 필요한지 이해한 뒤 쓴다.
- 보안(사용자 분리)과 금액 정확성(decimal)은 "나중에 고치기"가 가장 어려운 부분이므로 처음부터 지킨다.

---

## 15. 문서 관리

| 위치 | 내용 | 독자 |
|---|---|---|
| `CLAUDE.md` | Claude Code가 매 세션 읽는 작업 규칙 (짧게 유지) | Claude |
| `frontend/CLAUDE.md`, `backend/CLAUDE.md` | 해당 폴더 작업 시에만 읽히는 세부 규칙 | Claude |
| `README.md` | 프로젝트 소개, 실행 방법 | 사람 |
| `docs/development-plan.md` | 이 문서. 전체 계획, 결정 기록, 진행 현황 | 사람 + Claude |
| `docs/` 기타 | 내용이 커지면 이 문서에서 분리 (예: `database.md`, `api.md`, `deployment.md`) | 사람 + Claude |
| `docs/learning-guide.md` | 개념 설명 규칙 (무엇을 어디에 설명하나, 생략할 항목). `CLAUDE.md`에서 불러온다 | Claude + 사람 |
| `docs/learning/` | React/TS/C# 학습 노트 (선택) | 사람 |

규칙:
- 파일 이름은 **영문 소문자-하이픈** (`api-design.md`). 내용은 한국어. (Git/터미널에서 한글 파일명이 깨져 보이는 문제 방지)
- 한 섹션이 길어져 다른 문서처럼 다뤄야 할 때만 분리한다. 문서를 미리 만들어두지 않는다.
- 코드를 보면 알 수 있는 내용은 문서에 중복해서 쓰지 않는다.
