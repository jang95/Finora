# Finora 개인 금융관리 프로그램 개발 계획서

> 기준일: 2026-10-07  
> 현재 상태: Phase 1 완료 (라우팅 + 반응형 레이아웃). 다음은 Phase 2

### 변경 이력

| 날짜 | 내용 |
|---|---|
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
| 라우팅 | React Router | Phase 1 |
| 서버 데이터 관리 | TanStack Query | Phase 5 |
| 차트 | Recharts | Phase 10 |
| Backend | ASP.NET Core Web API (.NET 10 LTS, C#) | Phase 3 |
| ORM | Entity Framework Core + Npgsql | Phase 3 |
| Database | PostgreSQL | Phase 3 |
| 인증 | ASP.NET Core Identity + HttpOnly Cookie | Phase 4 |
| PC | Web / PWA | — |
| Android | PWA 우선, 필요 시 React Native 검토 | Phase 11 |
| PWA | vite-plugin-pwa | Phase 11 |
| 외부 데이터 | 주식/환율 금융 API | Phase 9 |

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
│  ├─ 총자산
│  ├─ 이번 달 수입 / 지출
│  ├─ 투자현황
│  └─ 주요 통계
│
├─ 가계부
│  ├─ 수입
│  ├─ 지출
│  ├─ 이체 (내 계좌 간 이동)
│  ├─ 카테고리
│  └─ 월별 조회
│
├─ 자산
│  ├─ 현금
│  ├─ 은행
│  ├─ 예금/적금
│  └─ 기타 자산
│
├─ 투자
│  ├─ 투자계좌
│  ├─ 보유종목
│  ├─ 매수/매도
│  ├─ 배당
│  ├─ 수익률
│  └─ 평가금액
│
├─ 통계
│  ├─ 월별 수입/지출
│  ├─ 자산 변화
│  ├─ 지출 비중
│  └─ 투자 성과
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
│  ├─ common/                Button, Input, Modal, MoneyText ...
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
| components | 재사용 UI | 직접 API를 호출하지 않는다. props로 받는다 |
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
- Id, UserId, Name, Type(Income/Expense), SortOrder
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
- 로그인 API에 요청 횟수 제한 (ASP.NET Core Rate Limiting)

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

가족·지인이 집 밖에서 Android로 접속하려면 서버가 인터넷에서 접근 가능해야 한다. **Phase 6(첫 배포) 전까지** 아래 중 하나를 결정한다.

| 방식 | 장점 | 단점 |
|---|---|---|
| A. 집 PC/미니PC + Tailscale | 인터넷에 공개되지 않아 가장 안전. 비용 거의 없음 | 사용자마다 Tailscale 앱 설치 필요. 집 PC가 꺼지면 사용 불가 |
| B. 클라우드 VM (NCP, AWS Lightsail 등) + 도메인 | 어디서나 접속, 설치 불필요 | 월 비용. 서버 보안 관리 책임 |
| C. PaaS (Azure App Service 등) + 관리형 PostgreSQL | 서버 관리 부담 적음 | 비용이 상대적으로 높음 |

공통 필요 사항: HTTPS 인증서, DB 백업(외부 저장소로 복사), 장애 시 로그 확인 방법.

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

### Phase 2 — Finora 화면 제작 (임시 데이터) ← **다음 작업**

Backend 없이 화면부터 만든다. 임시 데이터는 `mocks/`에 두고 **services를 통해서만** 사용한다. (결정 D5)

순서:

```text
Login → Dashboard → Transactions → Assets → Investments → Statistics → Settings
```

목표:
- 화면 구조 확정
- 공통 컴포넌트 (Button, Input, Modal, MoneyText)
- 처음부터 반응형 (PC / 모바일)
- `types/`에 데이터 구조 정의 → 8장 DB 설계와 맞추기

완료 기준:
- 모든 화면이 임시 데이터로 표시됨
- 가계부 화면에서 등록/수정/삭제가 화면상으로 동작 (새로고침하면 초기화되어도 됨)

---

### Phase 3 — Backend + Database 기반

v1의 Phase 3(Backend)과 Phase 4(DB)를 합쳤다. DB 없는 API는 확인할 것이 거의 없기 때문이다.

작업:
1. PostgreSQL 설치 (Windows 설치판 또는 Docker)
2. `backend/`에 ASP.NET Core Web API 프로젝트 생성
3. EF Core + Npgsql 연결, 첫 Migration
4. 공통 오류 처리 Middleware
5. Vite proxy 설정 (`/api` → Backend)
6. 테스트 프로젝트 생성

완료 기준:
- `GET /api/health`가 DB 연결 상태를 반환
- Frontend에서 해당 API 호출 결과를 화면에 표시

---

### Phase 4 — 회원가입 / 로그인 / 사용자 분리

**실제 금융 데이터를 저장하기 전에** 완성한다. (결정 D6)

작업:
- ASP.NET Core Identity + Cookie 인증
- 초대코드 회원가입
- 로그인 / 로그아웃 / 로그인 유지
- 현재 사용자 확인 (`GET /api/me`)
- Global Query Filter
- 로그인 안 된 상태에서 페이지 접근 시 로그인 화면으로 이동

완료 기준:
- 초대코드 없이는 가입 불가
- 로그인 5회 실패 시 잠금
- 사용자 분리 자동 테스트 통과

---

### Phase 5 — 가계부 완성 (첫 실제 기능)

Frontend ↔ Backend를 처음으로 실제로 연결하는 단계. 가계부 하나를 끝까지 완성하며 연결 방식을 익힌다.

API 예:

```text
GET    /api/transactions?month=2026-10
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
GET    /api/categories
```

기능:
- 수입 / 지출 / 이체 등록, 수정, 삭제
- 카테고리 관리
- 날짜 검색, 월별 조회, 월별 합계

작업:
- TanStack Query 도입
- `transactionService` 내부를 mock → 실제 API로 교체

완료 기준:
- 새로고침/재로그인 후에도 데이터 유지
- 다른 사용자로 로그인하면 내 데이터가 보이지 않음

---

### Phase 6 — 첫 배포

가계부만 있는 상태로 **가족이 실제로 써보게** 한다. 남은 기능을 만들기 전에 피드백을 받는 것이 목적이다.

작업:
- 10장 배포 방식 결정 → 결정 기록(3장)에 추가
- HTTPS, 운영 환경변수
- DB 자동 백업 + 복원 테스트

완료 기준:
- Android 폰에서 외부 네트워크로 접속·로그인 가능
- 백업 파일로 다른 DB에 복원 성공

---

### Phase 7 — 자산 관리

기능:
- 현금 / 은행 / 예금 / 적금 / 기타 자산
- 자산 합계
- 월말 잔액 스냅샷 (자산 변화 기록)
- 가계부 거래와 계좌 잔액 연동

---

### Phase 8 — 투자 관리

기능:
- 투자계좌, 종목 등록
- 매수 / 매도 / 배당 (수수료·세금 포함)
- 보유수량, 평균매수가 (거래내역으로 계산)
- 실현 / 미실현 손익, 수익률
- 외화 종목 (거래 시점 환율 저장)

완료 기준:
- 계산 로직 단위 테스트 통과 (매수 → 일부 매도 → 추가 매수 시나리오)
- 증권사 앱에 표시된 평균매수가와 일치

---

### Phase 9 — 외부 금융 API

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

### Phase 10 — 통계 / Dashboard 고도화

기능:
- 월별 수입/지출 그래프
- 카테고리별 지출 비중
- 자산 변화
- 투자 수익률, 자산 배분
- 월별 비교

---

### Phase 11 — PWA / Android 대응

반응형은 Phase 1부터 적용되어 있으므로, 이 단계는 **설치 가능한 앱**으로 만드는 작업이다.

1차 목표:
- vite-plugin-pwa (manifest, 아이콘, service worker)
- Android 홈 화면 설치, Windows 앱 설치

필요성이 확인된 후:
- React Native 등 별도 Android 앱 검토

처음부터 Android 전용 앱을 따로 만들지 않는다.

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
[진행] Phase 2   화면 제작 (임시 데이터)
[ ]    Phase 3   Backend + Database 기반
[ ]    Phase 4   회원가입 / 로그인 / 사용자 분리
[ ]    Phase 5   가계부 완성
[ ]    Phase 6   첫 배포
[ ]    Phase 7   자산
[ ]    Phase 8   투자
[ ]    Phase 9   외부 금융 API
[ ]    Phase 10  통계 / Dashboard
[ ]    Phase 11  PWA / Android
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
| `docs/learning/` | React/TS/C# 학습 노트 (선택) | 사람 |

규칙:
- 파일 이름은 **영문 소문자-하이픈** (`api-design.md`). 내용은 한국어. (Git/터미널에서 한글 파일명이 깨져 보이는 문제 방지)
- 한 섹션이 길어져 다른 문서처럼 다뤄야 할 때만 분리한다. 문서를 미리 만들어두지 않는다.
- 코드를 보면 알 수 있는 내용은 문서에 중복해서 쓰지 않는다.
