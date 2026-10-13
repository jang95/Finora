# Finora Backend

ASP.NET Core Web API (.NET 10) + EF Core + PostgreSQL. 구조는 `docs/development-plan.md` 7장.

## 실행 준비 (처음 한 번)

- 저장소 루트에서 `.env.example`을 `.env`로 복사해 값을 채우고 `docker compose up -d` (결정 D12)
- 접속 문자열은 User Secrets에 둔다. 코드·appsettings·커밋에 넣지 않는다.
  `dotnet user-secrets set "ConnectionStrings:Finora" "Host=localhost;Port=5432;Database=...;Username=...;Password=..." --project Finora.Api`
- 운영에서는 환경 변수 `ConnectionStrings__Finora`를 쓴다. (User Secrets는 Development에서만 읽힌다)
- `dotnet tool restore` — 저장소에 등록된 `dotnet-ef` 설치

## 명령어 (`backend/`에서)

- 실행: `dotnet watch --project Finora.Api` (http://localhost:5153, Frontend는 Vite proxy로 `/api` 호출)
- 테스트: `dotnet test`
- Migration 추가: `dotnet ef migrations add <Name> --project Finora.Api --output-dir Data/Migrations`
- DB 적용: `dotnet ef database update --project Finora.Api`

## 규칙

- 오류 응답은 ProblemDetails 형식. 처리되지 않은 예외는 `Middleware/GlobalExceptionHandler`가 500으로 바꾸고, 예외 내용은 로그에만 남긴다.
- Controller는 Entity를 그대로 반환하지 않고 DTO로 변환한다.
- 테스트는 `FinoraApiFactory`(WebApplicationFactory)로 메모리 서버를 띄운다. 기본은 연결되지 않는 가짜 DB라 Docker 없이 돌아간다.
- `dotnet watch`로 서버를 띄운 채 `dotnet test`를 하면 `Finora.Api.exe` 잠김으로 빌드가 실패한다. 서버를 끄거나 `--artifacts-path`로 다른 폴더에 빌드한다.
