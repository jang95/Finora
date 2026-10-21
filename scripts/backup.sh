#!/usr/bin/env bash
# DB 백업 (Phase 7-4, 결정 D20). 노트북(Ubuntu)에서 하루 한 번 자동으로 실행한다 (타이머 설정은 7-5).
#   scripts/backup.sh
#
# 1) 운영 DB(compose.prod.yaml의 db)를 pg_dump로 파일 하나에 저장한다
# 2) 노트북에는 최근 14일치만 남긴다
# 3) RCLONE_REMOTE가 설정돼 있으면 노트북 밖(클라우드)으로 복사하고, 30일 지난 파일은 지운다
#    → rclone의 crypt(암호화) remote를 쓴다. 클라우드 업체도 파일 내용을 볼 수 없다. (금융 데이터)
#
# 설정 (.env 또는 환경 변수):
#   BACKUP_DIR     노트북 안 백업 폴더 (기본: ~/finora-backups)
#   RCLONE_REMOTE  rclone 복사 위치. 예) finora-crypt:  (비워 두면 클라우드 복사를 건너뛴다)

# set -euo pipefail: 명령 하나라도 실패하면 바로 멈춘다 (-e), 없는 변수를 쓰면 오류 (-u),
#   파이프(a | b) 중간 명령의 실패도 실패로 본다 (pipefail). 백업이 "조용히 실패"하지 않게 한다.
set -euo pipefail

# 스크립트 위치 기준으로 저장소 루트로 이동한다 (어느 폴더에서 실행해도 같게)
cd "$(dirname "$0")/.."

# .env의 값(POSTGRES_USER 등)을 이 스크립트의 변수로 읽는다. set -a: 읽은 변수를 자동으로 export
set -a
# shellcheck source=/dev/null
source .env
set +a

BACKUP_DIR="${BACKUP_DIR:-$HOME/finora-backups}"
RCLONE_REMOTE="${RCLONE_REMOTE:-}"
LOCAL_KEEP_DAYS=14
REMOTE_KEEP_DAYS=30

mkdir -p "$BACKUP_DIR"
# 파일 이름에 날짜·시각을 넣는다. 예) finora-20261015-030000.dump
file="$BACKUP_DIR/finora-$(date +%Y%m%d-%H%M%S).dump"

echo "[backup] $(date '+%F %T') 시작 → $file"

# trap ... EXIT: 스크립트가 어떻게 끝나든 마지막에 실행한다. 실패로 중간에 멈춰도 반쪽 파일(.partial)을 지운다.
# (성공하면 아래 mv로 이름이 바뀌어 .partial이 없으므로 아무 일도 하지 않는다)
trap 'rm -f "$file.partial"' EXIT

# pg_dump -Fc: PostgreSQL 전용 압축 형식. pg_restore로 표 하나만 골라 복원하는 것도 된다.
# exec -T: 터미널 없이 실행해서 출력(백업 내용)을 그대로 파일로 받는다.
# 먼저 .partial로 받고, 성공하면 이름을 바꾼다 → 중간에 실패한 반쪽 파일이 "정상 백업"처럼 남지 않는다.
docker compose -f compose.prod.yaml exec -T db \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > "$file.partial"
# -s: 파일이 있고 크기가 0보다 크면 참
if [ ! -s "$file.partial" ]; then
  echo "[backup] 실패: 백업 파일이 비어 있습니다" >&2
  exit 1
fi
mv "$file.partial" "$file"
echo "[backup] 저장 완료 ($(du -h "$file" | cut -f1))"

# 노트북 안 오래된 백업 정리: 14일보다 오래된 .dump 파일 삭제
find "$BACKUP_DIR" -name 'finora-*.dump' -mtime +"$LOCAL_KEEP_DAYS" -delete

if [ -n "$RCLONE_REMOTE" ]; then
  # rclone copy: 같은 이름 파일이 이미 있으면 건너뛰고 새 파일만 올린다
  rclone copy "$file" "$RCLONE_REMOTE"
  # --min-age 30d: 30일보다 오래된 파일만 지운다
  rclone delete "$RCLONE_REMOTE" --min-age "${REMOTE_KEEP_DAYS}d" --include 'finora-*.dump'
  echo "[backup] 클라우드 복사 완료 → $RCLONE_REMOTE"
else
  echo "[backup] RCLONE_REMOTE가 없어 클라우드 복사를 건너뜁니다 (노트북 밖 백업 없음!)" >&2
fi

echo "[backup] $(date '+%F %T') 끝"
