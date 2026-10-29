# 노트북 준비 안내 (Phase 7-5, 결정 D20 · D21)

집 노트북을 Finora 운영 서버로 만드는 순서. 노트북을 새로 설치할 때마다 처음부터 따라 한다.
설치가 끝난 뒤 쓰는 명령어(배포·백업·복원)는 `docs/operations.md`.

- 운영체제: **Linux Mint 22.3 Xfce** (Ubuntu 24.04 LTS 기반, 결정 D21)
- 실제 비밀번호·Tailscale 주소·rclone 설정값은 이 문서에 적지 않는다. (Public 저장소)
- 명령은 노트북의 터미널에서 실행한다. `sudo`는 관리자 권한으로 실행한다는 뜻이고, 처음 한 번 내 계정 비밀번호를 묻는다.

## 1. 설치 직후: 업데이트

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y git curl
```

- `apt`: Ubuntu 계열의 프로그램 설치 도구. `update`로 최신 목록을 받고, `upgrade`로 설치된 프로그램을 최신 버전으로 바꾼다.
- `무시: ... zena InRelease`는 정상이다 (Mint 저장소는 `Release` 파일을 대신 쓴다). `E:`/`Err:` 줄이 없으면 성공.
- **`packages.linuxmint.com`에 연결할 수 없다**고 나오면: `ping -c 3 8.8.8.8`은 되는데 `ping -c 3 packages.linuxmint.com`이 안 되면 DNS 문제다.
  와이파이 아이콘 오른쪽 클릭 → 연결 편집 → 내 와이파이 → IPv4 설정 → 방식 "자동 (DHCP) 주소만", DNS 서버 `1.1.1.1, 8.8.8.8` → 저장 → **와이파이를 끊었다 다시 연결**.
  확인: `resolvectl query packages.linuxmint.com`에 IP가 나오면 된다.
- 자동 업데이트: 메뉴 → **업데이트 관리자** → 편집 → 기본 설정 → **자동화** 탭에서 "업데이트 자동 적용"을 켠다.
  (Mint는 Ubuntu의 unattended-upgrades 대신 자체 업데이트 관리자로 자동 업데이트를 한다)

## 2. 덮개를 닫아도, 오래 두어도 꺼지지 않게

서버는 덮개를 닫은 채 계속 켜져 있어야 한다. 두 곳 모두 설정한다.

1) 시스템 설정 (systemd-logind: 덮개 닫힘 같은 전원 이벤트를 처리하는 시스템 프로그램)

```bash
sudo mkdir -p /etc/systemd/logind.conf.d
printf '[Login]\nHandleLidSwitch=ignore\nHandleLidSwitchExternalPower=ignore\nHandleLidSwitchDocked=ignore\n' \
  | sudo tee /etc/systemd/logind.conf.d/finora-lid.conf
# 절전·최대 절전을 아예 못 하게 막는다 (mask: 해당 기능을 실행할 수 없게 잠근다)
sudo systemctl mask sleep.target suspend.target hibernate.target hybrid-sleep.target
```

2) Xfce 전원 관리자 (데스크톱이 따로 덮개를 처리하기 때문)
- 메뉴 → 설정 → **전원 관리자** → 일반: "노트북 덮개를 닫았을 때" → 전원 연결·배터리 모두 **화면 끄기**
- 시스템 탭: "비활성 시 절전" → **안 함**

확인: 재부팅한 뒤 덮개를 닫고 5분 뒤 다른 기기에서 `ping`하거나, 다시 열었을 때 터미널의 `uptime` 시간이 이어져 있으면 된다.

> 로그인하지 않아도 Docker와 Finora는 시스템 서비스로 시작된다. 정전 뒤 전원만 다시 켜면 된다.

3) 와이파이가 자주 끊길 때 (유선 연결이면 건너뛴다)

이 노트북의 Intel 6235 무선 카드(드라이버 `iwlwifi`)는 와이파이가 하루 수십 번 끊겼다 (로그에 `reason=4 locally_generated=1`). 끊긴 동안은 Finora 접속도 안 된다. 아래 두 가지를 끄고 재부팅하니 하루 약 50번 → 약 20번으로 줄었다 (완전히 없어지지는 않음).

```bash
# 와이파이 절전 끄기 (<와이파이 이름>: nmcli connection show 로 확인)
sudo nmcli connection modify <와이파이 이름> 802-11-wireless.powersave 2
# 드라이버 옵션: 블루투스 간섭 처리 끄기(bt_coex_active=0), 묶어 보내기 끄기(11n_disable=8)
echo options iwlwifi bt_coex_active=0 11n_disable=8 | sudo tee /etc/modprobe.d/iwlwifi-finora.conf
sudo reboot
```

- 확인: `cat /sys/module/iwlwifi/parameters/bt_coex_active` → `N`, `.../11n_disable` → `8`
- 끊긴 횟수: `journalctl -b -u NetworkManager | grep -c "completed -> disconnected"` (`-b`: 이번 부팅 이후, 오늘만 보려면 `-b` 대신 `--since today`)
- 남은 원인은 약한 신호다 (2.4GHz에서 약 -70 dBm, 주변 와이파이 10개 이상). USB 와이파이 동글로 5GHz에 붙여 봤지만 공유기와 멀어서 연결이 유지되지 않았다. 근본 해결은 노트북을 공유기 가까이 두거나 유선(LAN)으로 연결하는 것이다.
- `/etc/modprobe.d/`의 파일은 root 소유여야 한다 (`tee`로 만들면 root 소유). 다른 곳에서 만든 파일을 `mv`로 옮기면 내 계정 소유로 남으니 `sudo chown root:root <파일>`.

## 3. Docker 설치

Mint 기본 저장소의 Docker는 오래된 버전이라, Docker 공식 저장소에서 설치한다.

```bash
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
# Mint의 이름(zena)이 아니라 바탕이 된 Ubuntu 이름(noble)을 써야 한다 → UBUNTU_CODENAME
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$UBUNTU_CODENAME") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

`sudo` 없이 `docker`를 쓰도록 내 계정을 docker 그룹에 넣고, **로그아웃 후 다시 로그인**한다.
(docker 그룹은 관리자 권한과 거의 같다. 이 노트북은 서버 전용이라 괜찮다)

```bash
sudo usermod -aG docker "$USER"
```

확인:

```bash
docker run --rm hello-world     # "Hello from Docker!"가 나오면 성공
docker compose version
```

## 4. 저장소 받기 + DB 띄우기

```bash
git clone https://github.com/jang95/Finora.git ~/finora
cd ~/finora
cp .env.example .env
chmod 600 .env                  # 나만 읽고 쓸 수 있게 (비밀번호가 들어 있다)
# POSTGRES_PASSWORD=change-me 줄을 무작위 비밀번호로 바꾼다 (복사·붙여넣기 실수가 없게 자동으로)
sed -i "s/^POSTGRES_PASSWORD=.*/POSTGRES_PASSWORD=$(openssl rand -hex 24)/" .env
cat .env                        # change-me가 48자리 값으로 바뀌었는지 확인
```

- `.env` 내용은 노트북이 고장 나도 복원할 수 있게 **비밀번호 관리자 등 안전한 곳에 따로 보관**한다. (`docs/operations.md` 실제 복원 1번)
- `-hex`를 쓰는 이유: base64 값에 섞이는 `/ + =`가 접속 문자열에서 문제를 일으킬 수 있어서 0-9a-f만 나오게 한다.

먼저 DB만 띄워서 확인한다:

```bash
docker compose -f compose.prod.yaml up -d db
docker compose -f compose.prod.yaml ps          # db가 "healthy"가 될 때까지 몇 초 기다린다 (한 줄씩 실행)
docker compose -f compose.prod.yaml exec db sh -c 'psql -P pager=off -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "select version();"'
```

- `health: starting`일 때 psql을 실행하면 `connection to server on socket ... failed`가 난다. healthy가 된 뒤 다시 실행한다.
- `.env`를 화면에 띄운 채 사진·화면 공유하지 않는다. 비밀번호가 새어 나가면, **데이터가 없을 때만** `down -v`(DB 데이터 삭제) 후 비밀번호를 다시 만들고 `up -d db`.
  데이터가 생긴 뒤에는 `-v`를 절대 쓰지 않는다 (PostgreSQL은 처음 만들 때의 비밀번호를 데이터 안에 저장한다).

전체(Migration + API)를 띄운다. 처음 빌드는 이 노트북에서 10분 이상 걸릴 수 있다.

```bash
free -h                                          # Swap이 0이면 빌드 중 메모리가 부족할 수 있다 → 아래 참고 (Mint는 기본으로 약 4GB Swap 파일을 만든다)
docker compose -f compose.prod.yaml up -d --build
curl http://127.0.0.1:8080/api/health            # {"status":"ok","database":"connected"}
```

> Swap이 없으면: `sudo fallocate -l 4G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile`
> 후 `/etc/fstab`에 `/swapfile none swap sw 0 0` 한 줄을 추가한다. (Swap: RAM이 모자랄 때 디스크를 임시 메모리로 쓰는 공간)

백업·복원이 이 노트북에서 되는지 한 번 확인한다 (클라우드 복사는 아직 없다는 경고가 나오는 것이 정상):

```bash
scripts/backup.sh
scripts/restore-test.sh ~/finora-backups/finora-*.dump
```

## 5. 원격 접속 (Tailscale + SSH)

개발 PC에서 노트북 터미널을 쓰기 위한 설정. 공유기 포트는 열지 않고 **Tailscale 안에서만** SSH를 받는다.

1) 노트북과 개발 PC에 Tailscale을 설치하고 같은 계정으로 로그인한다. (tailscale.com/download)
   개발 PC에서 `tailscale status`에 두 기기가 보이면 된다.

2) 노트북: SSH 서버 설치

```bash
sudo apt install -y openssh-server
sudo systemctl enable --now ssh
```

3) 개발 PC(PowerShell): 키를 만들고 노트북에 공개 키를 등록한다. 비밀번호는 이때 한 번만 입력한다.
   `<노트북>` = `tailscale status`에 보이는 노트북의 100.x 주소 (문서·커밋에 적지 않는다)

```powershell
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\finora_laptop -C finora-laptop-from-desktop
type $env:USERPROFILE\.ssh\finora_laptop.pub | ssh jj-server@<노트북> "mkdir -p ~/.ssh && chmod 700 ~/.ssh && cat >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys"
ssh -i $env:USERPROFILE\.ssh\finora_laptop jj-server@<노트북> hostname   # 비밀번호 없이 이름이 나오면 성공
```

4) 노트북: 비밀번호 로그인 끄기 (**3)이 성공한 뒤에만**. 먼저 끄면 접속할 방법이 없어진다)

```bash
printf 'PasswordAuthentication no\nKbdInteractiveAuthentication no\nPermitRootLogin no\n' \
  | sudo tee /etc/ssh/sshd_config.d/10-finora.conf
sudo sshd -t && sudo systemctl restart ssh      # sshd -t: 설정 문법 검사. 오류가 없을 때만 재시작
```

5) 노트북: 방화벽(ufw) — SSH는 Tailscale(`tailscale0`)로 들어온 연결만 받는다

```bash
sudo ufw default deny incoming                  # 밖에서 먼저 들어오는 연결은 기본으로 막는다
sudo ufw default allow outgoing                 # 노트북이 밖으로 나가는 연결(업데이트·백업)은 허용
sudo ufw allow in on tailscale0 to any port 22 proto tcp
sudo ufw enable
sudo ufw status verbose
```

- Finora(`127.0.0.1:8080`)는 노트북 자신만 접속하므로 ufw와 관계없다. Tailscale Funnel도 노트북 안에서 전달하므로 막히지 않는다.

## 6. 인터넷 공개 (Tailscale Funnel)

가족이 Tailscale 없이 `https://finora.<tailnet>.ts.net` 으로 접속하게 한다. 공유기 포트포워딩은 하지 않는다 (노트북이 Tailscale 쪽으로 먼저 연결해 두고 요청을 받아 온다).
Funnel은 무료 Personal 요금제(비상업적 사용)에서 쓸 수 있다.

1) 노트북: 기기 이름을 짧게 (공개 주소의 앞부분이 된다)

```bash
sudo tailscale set --hostname=finora
```

2) 노트북: Funnel 켜기 — 공개 주소로 온 요청을 `127.0.0.1:8080`(Finora)으로 넘긴다

```bash
sudo tailscale funnel --bg 8080                 # --bg: 터미널을 닫거나 재부팅해도 유지
tailscale funnel status                         # "Funnel on" + 주소가 나오면 성공
```

- 처음이면 "Funnel not available"과 승인 링크가 나온다. 링크를 열어 tailnet의 HTTPS·Funnel을 허용하고 명령을 다시 실행한다.
- HTTPS 인증서는 Tailscale이 자동으로 받고 갱신한다. 처음 켠 직후 몇 분은 접속이 실패할 수 있다.
- 끄기: `sudo tailscale funnel reset` (공개 설정 전체 삭제)

3) 확인
- Android 폰: Wi-Fi를 끄고(모바일 데이터) 주소에 접속 → 로그인 화면이 보이면 성공
- 접속자 IP 확인(7-2): Funnel 주소로 틀린 로그인을 1분에 11번 → 11번째 429. 바로 노트북에서 `127.0.0.1:8080`으로 1번 → 401이면 사람마다 IP가 구분되는 것 (429면 모두 프록시 IP 하나로 묶인 것)

## 7. 클라우드 백업 (rclone crypt) + 매일 자동 실행

`scripts/backup.sh`가 만든 백업을 **암호화해서** Google Drive에 복사하고, 매일 03:30에 자동으로 실행한다.
rclone: 클라우드 저장소를 `이름:폴더` 형식으로 다루는 복사 도구. 아래에서 sudo가 붙은 명령은 1)과 5)의 두 개뿐이다.

1) 설치

```bash
sudo apt install -y rclone
```

2) Google Drive 연결 (`gdrive` remote) — **노트북 화면의 터미널**에서 (브라우저가 열린다, sudo 없이)

```bash
rclone config create gdrive drive scope=drive.file
```

- Firefox에서 백업용 구글 계정으로 로그인 → 허용 → "Success"가 나오면 끝.
- `scope=drive.file`: rclone이 **자기가 올린 파일만** 볼 수 있다. 드라이브의 다른 파일에는 손대지 못한다.
- 마지막에 출력되는 `token = {...}`은 드라이브 접속 열쇠다. 사진 찍거나 공유하지 않는다.
- 확인: `rclone about gdrive:` → 드라이브 용량이 나오면 성공.

3) 암호화 층 (`finora-crypt` remote) — `gdrive:finora-backups` 폴더를 감싸서, 넣는 파일의 내용과 이름을 암호화한다

```bash
rclone config create finora-crypt crypt remote=gdrive:finora-backups \
  password="$(openssl rand -base64 32)" password2="$(openssl rand -base64 32)" --obscure > /dev/null
```

- 비밀번호 2개를 무작위로 만들어 화면에 보이지 않게 바로 설정에 넣는다. `> /dev/null`: 출력(설정 내용)을 버린다.
- 확인: 파일을 하나 넣어 보면 `rclone ls finora-crypt:`에는 원래 이름이, `rclone ls gdrive:finora-backups`에는 알아볼 수 없는 이름이 보인다.
- ⚠️ **이 비밀번호를 잃으면 클라우드 백업은 아무도 풀 수 없다.** `~/.config/rclone/rclone.conf`(드라이브 열쇠 + 암호화 키)를 노트북 밖 안전한 곳(USB, 비밀번호 관리자 등)에 복사해 둔다. 저장소·메신저에는 올리지 않는다. 노트북이 고장 나면 다른 컴퓨터에 rclone을 설치하고 이 파일을 같은 위치에 두면 백업을 내려받을 수 있다.

4) 백업 스크립트에 연결 — `.env` 맨 아래에 한 줄 추가하고 직접 한 번 실행

```bash
echo 'RCLONE_REMOTE=finora-crypt:' >> .env
scripts/backup.sh                          # 마지막에 "클라우드 복사 완료"가 나오면 성공
```

- 클라우드에서 다시 받아 복원도 되는지 확인한다: `docs/operations.md` 백업 항목.

5) 매일 자동 실행 (systemd timer)

systemd timer: 리눅스의 예약 실행(cron과 비슷). `scripts/systemd/`의 두 파일을 짝으로 쓴다.
`finora-backup.service`는 무엇을(`backup.sh` 한 번), `finora-backup.timer`는 언제(매일 03:30, 꺼져 있었으면 다음 부팅 때) 실행할지 정한다.
내 계정용(user unit)으로 설치해서 관리·로그 확인에 sudo가 필요 없다.

```bash
sudo loginctl enable-linger $USER          # 로그인하지 않아도 내 계정의 타이머가 돌게 한다 (처음 한 번)
mkdir -p ~/.config/systemd/user
cp scripts/systemd/finora-backup.* ~/.config/systemd/user/
systemctl --user daemon-reload             # 새 파일을 읽게 한다
systemctl --user enable --now finora-backup.timer   # 켜고, 재부팅 후에도 켜지게
systemctl --user list-timers               # NEXT에 다음 실행 시각(03:30)이 보이면 성공
```

- 지금 한 번 실행해 보기: `systemctl --user start finora-backup.service` (끝날 때까지 기다린다)
- 실행 기록: `journalctl --user -u finora-backup.service` → `[backup] ... 끝`이 있으면 성공
- `scripts/systemd/`의 파일을 고치면 다시 `cp`하고 `daemon-reload`한다.
