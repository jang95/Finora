# Finora 운영 안내 (집 노트북, 결정 D20)

운영 서버(노트북)에서 쓰는 명령어 모음. 모든 명령은 **저장소 루트**에서 실행한다.
실제 비밀번호·초대코드·Tailscale 주소·백업 파일은 이 문서에 적지 않는다. (Public 저장소)

## 배포

| 할 일 | 명령어 |
|---|---|
| 처음 배포 / 새 버전 배포 | `git pull` 후 `docker compose -f compose.prod.yaml up -d --build` |
| 상태 보기 | `docker compose -f compose.prod.yaml ps` |
| 로그 보기 | `docker compose -f compose.prod.yaml logs -f api` |
| 초대코드 발급 | `docker compose -f compose.prod.yaml run --rm api invite create` (코드는 어디에도 기록하지 않는다) |

배포하면 `migrate`가 DB 구조를 먼저 적용하고, 성공해야 `api`가 시작된다. 로그인 Cookie 키는 volume에 있어서 배포해도 로그아웃되지 않는다.

## 백업

- `scripts/backup.sh`: DB를 `~/finora-backups`에 저장(14일치 보관)하고, `RCLONE_REMOTE`가 있으면 클라우드에 **암호화해서** 복사한다(30일치 보관).
- 하루 한 번 자동 실행은 7-5에서 설정한다.
- **한 달에 한 번** 복원 테스트를 한다. 클라우드에서 내려받은 파일로도 해 본다.
  ```bash
  scripts/restore-test.sh ~/finora-backups/finora-YYYYMMDD-HHMMSS.dump
  ```
  운영 DB는 건드리지 않고, 임시 DB에 복원해서 표별 행 수를 보여준 뒤 임시 DB를 지운다.

## 실제 복원 (노트북 고장 · DB 손상 시)

운영 DB를 백업 시점으로 **되돌리는** 작업이다. 백업 이후 입력한 거래는 사라진다.

1. 새 노트북(또는 고친 노트북)에 7-5 준비를 다시 하고, 저장소와 `.env`를 준비한다. (`.env` 값은 따로 안전한 곳에 보관해 둔다)
2. 클라우드에서 최신 백업을 내려받는다: `rclone copy <RCLONE_REMOTE>finora-YYYYMMDD-HHMMSS.dump ~/restore/`
3. 먼저 `scripts/restore-test.sh`로 그 파일이 정상인지 확인한다.
4. api를 멈추고 DB만 띄운다:
   ```bash
   docker compose -f compose.prod.yaml stop api
   docker compose -f compose.prod.yaml up -d db
   ```
5. 복원한다 (`--clean`: 지금 있는 표를 지우고 백업 내용으로 다시 만든다):
   ```bash
   set -a; source .env; set +a
   docker compose -f compose.prod.yaml exec -T db \
     pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner < ~/restore/finora-YYYYMMDD-HHMMSS.dump
   ```
6. 다시 배포한다: `docker compose -f compose.prod.yaml up -d` → 로그인해서 거래가 보이는지 확인한다.

새 노트북이면 Data Protection 키 volume이 없으므로 가족 모두 한 번 다시 로그인해야 한다. (데이터는 그대로)
