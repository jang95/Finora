# 운영용 이미지 (Phase 7, 결정 D20). React 빌드 파일과 ASP.NET Core API를 이미지 하나에 담는다.
# multi-stage build: 단계(FROM)마다 다른 이미지로 작업하고, 마지막 단계에는 실행에 필요한 결과물만 복사한다.
#   → Node.js나 .NET SDK(빌드 도구)는 최종 이미지에 들어가지 않아 작고 안전하다.
# 빌드: docker compose -f compose.prod.yaml build   (저장소 루트에서)

# ---- 1단계: React 빌드 (frontend/dist) ----
FROM node:22-alpine AS frontend
WORKDIR /src/frontend
# package.json만 먼저 복사해서 설치한다. 소스만 바뀌면 이 단계는 캐시를 그대로 써서 빌드가 빨라진다.
COPY frontend/package.json frontend/package-lock.json ./
# npm ci: package-lock.json에 적힌 버전 그대로 설치한다 (npm install보다 재현성이 높다)
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---- 2단계: API 빌드 (dotnet publish) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/Finora.Api/Finora.Api.csproj backend/Finora.Api/
RUN dotnet restore backend/Finora.Api/Finora.Api.csproj
COPY backend/Finora.Api/ backend/Finora.Api/
RUN dotnet publish backend/Finora.Api/Finora.Api.csproj -c Release -o /app --no-restore

# ---- 3단계: 실행용 이미지 (ASP.NET Core 런타임만) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app ./
# React 빌드 결과를 wwwroot에 넣는다 → Program.cs의 UseStaticFiles / MapFallbackToFile이 제공한다 (7-1)
COPY --from=frontend /src/frontend/dist ./wwwroot
# Data Protection 키 폴더 (compose에서 volume으로 연결). 아래 app 사용자가 쓸 수 있게 미리 만들어 둔다.
RUN mkdir /keys && chown app:app /keys
# app: .NET 이미지에 들어 있는 일반 사용자. root로 실행하지 않아서, 앱이 뚫려도 컨테이너 안에서 할 수 있는 일이 줄어든다.
USER app
# .NET 8부터 컨테이너 기본 포트는 8080 (ASPNETCORE_HTTP_PORTS)
EXPOSE 8080
# "docker compose run --rm api invite create"처럼 뒤에 붙인 값은 args로 들어간다 (Program.cs 명령어 모드)
ENTRYPOINT ["dotnet", "Finora.Api.dll"]
