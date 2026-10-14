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
- 초대코드 발급: `dotnet run --project Finora.Api -- invite create` (결정 D14. 출력된 코드는 어디에도 기록하지 않는다)

## 규칙

- 오류 응답은 ProblemDetails 형식. 처리되지 않은 예외는 `Middleware/GlobalExceptionHandler`가 500으로 바꾸고, 예외 내용은 로그에만 남긴다.
- Controller는 Entity를 그대로 반환하지 않고 DTO로 변환한다.
- 규칙은 `Services/`에 두고 `ServiceResult`(성공 / 404 / 400 입력칸 오류 / 409 충돌)로 돌려준다. Controller는 그 결과를 HTTP 응답으로만 바꾼다.
- 사용자 소유 테이블은 `UserId` + Global Query Filter(`FinoraDbContext`). 요청에 들어온 다른 테이블 Id(카테고리 등)도 내 것인지 확인하고, 분리 테스트를 `UserIsolationTests`에 추가한다.
- 테스트는 WebApplicationFactory로 메모리 서버를 띄운다. DB가 필요 없으면 `FinoraApiFactory`(연결되지 않는 가짜 DB), 필요하면 `PostgresApiFactory`(Testcontainers로 테스트용 PostgreSQL 컨테이너, 결정 D13). **`dotnet test`는 Docker가 켜져 있어야 한다.**
- `dotnet watch`로 서버를 띄운 채 `dotnet test`를 하면 `Finora.Api.exe` 잠김으로 빌드가 실패한다. 서버를 끄거나 `--artifacts-path`로 다른 폴더에 빌드한다. `dotnet ef`는 `--configuration Release`를 붙이면 잠기지 않는 bin/Release에 빌드한다.
