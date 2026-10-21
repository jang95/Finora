#!/usr/bin/env bash
# 백업 복원 테스트 (Phase 7-4 완료 기준: "백업 파일로 다른 DB에 복원 성공")
#   scripts/restore-test.sh ~/finora-backups/finora-20261015-030000.dump
#
# 운영 DB는 건드리지 않는다. 잠깐 쓰고 지우는 PostgreSQL 컨테이너를 새로 띄워서 거기에 복원하고,
# 표마다 행 수를 보여준 뒤 컨테이너를 지운다. 백업은 "복원이 되는지 확인해야" 진짜 백업이다. (계획서 9장)
# 한 달에 한 번쯤, 클라우드에서 내려받은 파일로도 해 본다.
set -euo pipefail

if [ $# -ne 1 ] || [ ! -s "$1" ]; then
  echo "사용법: scripts/restore-test.sh <백업파일.dump>" >&2
  exit 1
fi
dump="$1"

container="finora-restore-test-$$"   # $$ = 이 스크립트의 프로세스 번호 (이름이 겹치지 않게)
# 테스트용 컨테이너 안에서만 쓰는 임시 비밀번호 (밖에 포트를 열지 않고, 끝나면 지운다)
password="restore-test-$RANDOM$RANDOM"

# trap ... EXIT: 스크립트가 어떻게 끝나든(성공·실패·Ctrl+C) 마지막에 컨테이너를 지운다
trap 'docker rm -f "$container" > /dev/null 2>&1 || true' EXIT

echo "[restore-test] 임시 DB 시작"
docker run -d --name "$container" -e POSTGRES_PASSWORD="$password" -e POSTGRES_DB=finora postgres:18 > /dev/null

# 접속을 받을 준비가 될 때까지 최대 30초 기다린다
for _ in $(seq 1 30); do
  if docker exec "$container" pg_isready -U postgres -d finora > /dev/null 2>&1; then break; fi
  sleep 1
done

echo "[restore-test] 복원: $dump"
# 백업 파일 내용을 컨테이너의 pg_restore로 보낸다 (-i: 입력을 컨테이너로 전달)
# --no-owner: 원래 DB의 사용자 이름(POSTGRES_USER)이 이 임시 DB에 없어도 복원되게 소유자 정보는 무시한다
# --exit-on-error: 하나라도 실패하면 바로 실패로 끝낸다
docker exec -i "$container" pg_restore -U postgres -d finora --no-owner --exit-on-error < "$dump"

echo "[restore-test] 표별 행 수"
# 주요 표의 행 수를 보여준다. 운영 화면에서 보던 거래 수와 비슷한지 눈으로 확인한다.
docker exec "$container" psql -U postgres -d finora -c "
  SELECT 'AspNetUsers' AS table, count(*) FROM \"AspNetUsers\"
  UNION ALL SELECT 'Categories', count(*) FROM \"Categories\"
  UNION ALL SELECT 'Transactions', count(*) FROM \"Transactions\"
  UNION ALL SELECT '__EFMigrationsHistory', count(*) FROM \"__EFMigrationsHistory\";"

echo "[restore-test] 성공 (임시 DB는 지웁니다)"
