# WMS v1.0 — Windows + Docker 部署 SOP

適用於在 Windows 上以 Docker Desktop 部署 WMS。三個容器分別是資料庫、後端 API 與
前端（內含 Nginx），彼此透過 Docker 內部網路通訊，對外只開放一個連接埠。

| 項目 | 版本 / 說明 |
|---|---|
| 作業系統 | Windows 10 21H2 以上 / Windows 11 / Windows Server 2022 |
| 容器平台 | Docker Desktop 4.30 以上（WSL2 後端） |
| 容器 | `db`（PostgreSQL 17）、`api`（ASP.NET Core 10）、`web`（Nginx + Vue SPA） |
| 對外埠 | 8080（可調整） |

---

## 架構

```text
            瀏覽器 → http://localhost:8080
                        │
        ┌───────────────┴───────────────┐
        │      web（nginx:alpine）       │
        │                               │
        │  /       → Vue SPA 靜態檔案    │
        │  /api/   → http://api:8080    │
        └───────────────┬───────────────┘
                        │  Docker 內部網路 wms-net
                        ↓
        ┌───────────────────────────────┐
        │  api（ASP.NET Core，:8080）    │
        └───────────────┬───────────────┘
                        │
                        ↓
        ┌───────────────────────────────┐
        │  db（PostgreSQL，:5432）       │
        │  資料存於具名 volume wms-data   │
        └───────────────────────────────┘
```

只有 `web` 對外開放連接埠，`api` 與 `db` 都在內部網路，不直接暴露。

---

## 步驟一：安裝 Docker Desktop

1. 以系統管理員開啟 PowerShell，啟用 WSL2：

   ```powershell
   wsl --install
   wsl --set-default-version 2
   ```

   如提示需要重新開機，請先重開。

2. 下載並安裝 Docker Desktop：<https://www.docker.com/products/docker-desktop/>
   安裝時勾選 **Use WSL 2 instead of Hyper-V**。

3. 啟動 Docker Desktop，等待左下角狀態變成 **Engine running**。

4. 確認版本：

   ```powershell
   docker --version
   docker compose version
   ```

---

## 步驟二：取得程式碼

```powershell
cd D:\Gitlab
git clone https://github.com/LenShi1989/WMS.git
cd WMS
git checkout main
```

若專案已在本機（例如 `D:\Gitlab\WMS`），直接切換過去即可。

---

## 步驟三：建立部署檔案

以下檔案都放在 `D:\Gitlab\WMS\deploy\docker\`，`.dockerignore` 除外（放在專案根目錄）。

### 3-1. `deploy/docker/Dockerfile.api`

```dockerfile
# ---------- 建置階段 ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先只複製專案檔，讓 restore 結果能被 Docker 快取
COPY src/Wms.Domain/Wms.Domain.csproj           src/Wms.Domain/
COPY src/Wms.Application/Wms.Application.csproj src/Wms.Application/
COPY src/Wms.Infrastructure/Wms.Infrastructure.csproj src/Wms.Infrastructure/
COPY src/Wms.Api/Wms.Api.csproj                 src/Wms.Api/
RUN dotnet restore src/Wms.Api/Wms.Api.csproj

# 再複製原始碼並發佈
COPY src/ src/
RUN dotnet publish src/Wms.Api/Wms.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---------- 執行階段 ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# healthcheck 需要 curl
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# 使用映像內建的非 root 帳號
USER app

HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "Wms.Api.dll"]
```

### 3-2. `deploy/docker/Dockerfile.web`

```dockerfile
# ---------- 建置階段 ----------
FROM node:20-alpine AS build
WORKDIR /app

COPY wms-web/package.json wms-web/package-lock.json ./
RUN npm ci

COPY wms-web/ ./
RUN npm run build

# ---------- 執行階段 ----------
FROM nginx:alpine AS final

COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1/ >/dev/null 2>&1 || exit 1
```

> 前端的 `VITE_API_BASE_URL` 預設是相對路徑 `/api/v1`，由同容器的 Nginx 代理到
> `api` 服務，因此**不需要**在建置時指定 API 網址。

### 3-3. `deploy/docker/nginx.conf`

```nginx
upstream wms_api {
    server api:8080;
    keepalive 32;
}

server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    client_max_body_size 20m;

    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_types text/plain text/css text/xml application/json application/javascript
               application/xml+rss image/svg+xml;

    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "SAMEORIGIN" always;

    # 帶雜湊的靜態資源可長期快取
    location /assets/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    location /api/ {
        proxy_pass http://wms_api;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }

    location /health {
        proxy_pass http://wms_api;
        access_log off;
    }

    # Swagger：正式環境建議關閉。要開放時把 return 404 註解掉。
    location /swagger/ {
        return 404;
        # proxy_pass http://wms_api;
        # proxy_set_header Host $host;
    }

    # SPA 路由：找不到實體檔案一律回 index.html
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

### 3-4. `deploy/docker/compose.yaml`

```yaml
name: wms

services:
  db:
    image: postgres:17-alpine
    container_name: wms-db
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      TZ: Asia/Taipei
    volumes:
      - wms-data:/var/lib/postgresql/data
    networks:
      - wms-net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 20s
    # 僅在需要用外部工具連線時才打開；正式環境請維持註解
    # ports:
    #   - "15432:5432"

  api:
    build:
      context: ../..
      dockerfile: deploy/docker/Dockerfile.api
    container_name: wms-api
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      Jwt__Issuer: WmsApi
      Jwt__Audience: WmsWeb
      Jwt__AccessTokenMinutes: 60
      Cors__AllowedOrigins__0: ${PUBLIC_ORIGIN}
      Database__AutoMigrate: "true"
      TZ: Asia/Taipei
    networks:
      - wms-net

  web:
    build:
      context: ../..
      dockerfile: deploy/docker/Dockerfile.web
    container_name: wms-web
    restart: unless-stopped
    depends_on:
      api:
        condition: service_healthy
    ports:
      - "${PUBLIC_PORT}:80"
    networks:
      - wms-net

networks:
  wms-net:
    driver: bridge

volumes:
  wms-data:
```

### 3-5. `deploy/docker/.env`

這個檔案存放機密，**不要進版控**（專案根目錄的 `.gitignore` 已排除 `.env`）。

```dotenv
# 資料庫
POSTGRES_DB=wms
POSTGRES_USER=wms
POSTGRES_PASSWORD=請改成強式密碼

# JWT 簽章金鑰，至少 32 字元
JWT_SECRET_KEY=請改成隨機產生的長字串

# 對外連接埠與前端來源
PUBLIC_PORT=8080
PUBLIC_ORIGIN=http://localhost:8080
```

產生安全的 JWT 金鑰（PowerShell）：

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

同時建立一份不含機密的 `deploy/docker/.env.example` 供團隊參考，把密碼部分留空即可。

### 3-6. 專案根目錄 `.dockerignore`

避免把 `node_modules`、`bin`、`obj` 送進建置環境，可大幅縮短建置時間：

```gitignore
**/bin/
**/obj/
**/node_modules/
**/dist/
**/.vs/
**/.vscode/
**/.idea/
.git/
.gitignore
**/*.user
**/.env
**/.env.*
!**/.env.example
deploy/
tests/
*.md
```

---

## 步驟四：建置與啟動

```powershell
cd D:\Gitlab\WMS\deploy\docker

# 建置映像（第一次約需 3～8 分鐘）
docker compose build

# 啟動
docker compose up -d

# 查看狀態，三個服務都應為 healthy 或 running
docker compose ps
```

預期輸出類似：

```text
NAME      IMAGE          STATUS                   PORTS
wms-db    postgres:17-alpine   Up (healthy)
wms-api   wms-api        Up (healthy)
wms-web   wms-web        Up (healthy)             0.0.0.0:8080->80/tcp
```

首次啟動時 `api` 會自動建立 26 張資料表並寫入初始資料（42 項權限、4 種角色、
管理員帳號、示範倉儲資料）。可從日誌確認：

```powershell
docker compose logs api --tail 40
```

看到 `WMS 初始資料檢查完成。` 與 `Now listening on: http://[::]:8080` 即代表成功。

---

## 步驟五：驗證

```powershell
# 1. 健康檢查
curl.exe -s http://localhost:8080/health

# 2. 登入 API
curl.exe -s -X POST http://localhost:8080/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d '{\"username\":\"admin\",\"password\":\"a12345678\"}'

# 3. 前端首頁與 SPA 深層路由（都應回 200）
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:8080/
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:8080/inventory
```

接著用瀏覽器開啟 <http://localhost:8080>，以 `admin` / `a12345678` 登入，
確認儀表板、物料、入庫、庫存等頁面可正常操作。

> **登入後第一件事：立刻變更 admin 密碼**（右上角選單 → 個人資料 → 變更密碼）。

---

## 日常操作

```powershell
cd D:\Gitlab\WMS\deploy\docker

# 查看即時日誌
docker compose logs -f api
docker compose logs -f web

# 重新啟動單一服務
docker compose restart api

# 停止（保留資料）
docker compose stop

# 啟動
docker compose start

# 停止並移除容器（資料仍保留在 volume 中）
docker compose down

# 進入容器排查
docker compose exec api bash
docker compose exec db psql -U wms -d wms

# 查看資源用量
docker stats --no-stream
```

> `docker compose down -v` 會**連同 volume 一起刪除**，等於清空整個資料庫。
> 除非確定要重來，否則不要加 `-v`。

---

## 更新版本

```powershell
cd D:\Gitlab\WMS
git pull

cd deploy\docker
docker compose build --pull
docker compose up -d

# 確認
docker compose ps
curl.exe -s http://localhost:8080/health
```

資料庫結構變更會在 `api` 啟動時由 `Database__AutoMigrate` 自動套用。
**更新前請務必先備份**（見下一節）。

清掉不再使用的舊映像：

```powershell
docker image prune -f
```

---

## 備份與還原

### 備份

```powershell
cd D:\Gitlab\WMS\deploy\docker
New-Item -ItemType Directory -Force -Path D:\Backups\wms | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
docker compose exec -T db pg_dump -U wms -d wms -F c > "D:\Backups\wms\wms_$stamp.dump"

Get-ChildItem D:\Backups\wms\wms_*.dump | Select-Object Name, Length, LastWriteTime
```

### 排程每日備份

建立 `D:\Gitlab\WMS\deploy\docker\backup.ps1`：

```powershell
$ErrorActionPreference = 'Stop'

$composeDir = 'D:\Gitlab\WMS\deploy\docker'
$backupDir  = 'D:\Backups\wms'
$stamp      = Get-Date -Format 'yyyyMMdd_HHmmss'

New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
Push-Location $composeDir
try {
    docker compose exec -T db pg_dump -U wms -d wms -F c > "$backupDir\wms_$stamp.dump"

    # 只保留最近 14 天
    Get-ChildItem "$backupDir\wms_*.dump" |
        Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-14) } |
        Remove-Item -Force
}
finally {
    Pop-Location
}
```

註冊工作排程器（以系統管理員執行）：

```powershell
$action  = New-ScheduledTaskAction -Execute 'powershell.exe' `
           -Argument '-NoProfile -ExecutionPolicy Bypass -File "D:\Gitlab\WMS\deploy\docker\backup.ps1"'
$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

Register-ScheduledTask -TaskName 'WMS-Daily-Backup' `
  -Action $action -Trigger $trigger -RunLevel Highest -User 'SYSTEM'
```

### 還原

```powershell
cd D:\Gitlab\WMS\deploy\docker

# 停掉 API，避免還原期間有連線占用資料庫
docker compose stop api

# 重建空資料庫
docker compose exec -T db psql -U wms -d postgres -c "DROP DATABASE IF EXISTS wms;"
docker compose exec -T db psql -U wms -d postgres -c "CREATE DATABASE wms OWNER wms;"

# 匯入備份
Get-Content D:\Backups\wms\wms_20260927_020000.dump -AsByteStream -Raw |
  docker compose exec -T db pg_restore -U wms -d wms

docker compose start api
curl.exe -s http://localhost:8080/health
```

---

## 正式環境的額外設定

### 對外提供服務

若要讓區域網路或網際網路存取，除了調整 `PUBLIC_PORT` 與 `PUBLIC_ORIGIN`，
還需開放 Windows 防火牆：

```powershell
New-NetFirewallRule -DisplayName 'WMS Web 8080' -Direction Inbound `
  -Protocol TCP -LocalPort 8080 -Action Allow
```

並把 `.env` 的 `PUBLIC_ORIGIN` 改成實際網址，例如 `http://192.168.1.50:8080`，
改完後重啟：

```powershell
docker compose up -d
```

### 啟用 HTTPS

正式環境請在 `web` 之前再放一層反向代理（IIS、Caddy 或另一個 Nginx 容器）處理
TLS 憑證，並把 `PUBLIC_ORIGIN` 改成 `https://` 網址。不建議把憑證直接塞進應用容器。

### 開機自動啟動

在 Docker Desktop 設定中啟用 **Start Docker Desktop when you sign in**。
compose 服務都已設定 `restart: unless-stopped`，Docker 啟動後會自動把容器拉起來。

> Windows Server 環境建議改用 Docker Engine + 服務模式，不要依賴需要登入的 Docker Desktop。

---

## 疑難排解

| 現象 | 處理方式 |
|---|---|
| `docker compose build` 卡在 restore 或 npm ci | 網路問題。確認可連外，或在 Docker Desktop 設定代理伺服器 |
| `api` 一直重啟 | `docker compose logs api`。多半是 `.env` 的 `JWT_SECRET_KEY` 未填或不足 32 字元 |
| `api` 顯示連不到資料庫 | 確認連線字串的 Host 是 **`db`**（服務名）而不是 `localhost` |
| 前端可開但 API 回 502 | `api` 尚未 healthy。`docker compose ps` 確認狀態，再看 `logs api` |
| 重新整理頁面出現 404 | `nginx.conf` 缺少 `try_files $uri $uri/ /index.html` |
| 埠已被占用 | `netstat -ano \| findstr :8080` 找出占用程序，或改 `.env` 的 `PUBLIC_PORT` |
| 改了 `.env` 沒生效 | 需重新建立容器：`docker compose up -d --force-recreate` |
| 資料不見了 | 確認是否執行過 `docker compose down -v`。用 `docker volume ls` 檢查 `wms_wms-data` 是否還在 |
| WSL2 記憶體占用過高 | 在 `%USERPROFILE%\.wslconfig` 加入 `[wsl2]` 與 `memory=4GB` 後執行 `wsl --shutdown` |
| 庫存操作回 409 | 這是正常的業務防護（庫存不足或並行衝突），依畫面訊息處理即可 |

---

## 正式上線前檢查清單

- [ ] `admin` 預設密碼 `a12345678` **已變更**
- [ ] `.env` 的 `POSTGRES_PASSWORD` 與 `JWT_SECRET_KEY` 已改為正式值
- [ ] `.env` **未**被提交到版控
- [ ] `compose.yaml` 中 `db` 的 `ports` 保持註解，資料庫不對外
- [ ] `nginx.conf` 的 `/swagger/` 維持 `return 404`（或已限制來源）
- [ ] `PUBLIC_ORIGIN` 與實際存取網址一致（含協定與連接埠）
- [ ] 每日備份排程已設定，且**實際測試過還原流程**
- [ ] 已確認 Docker Desktop 開機自動啟動
- [ ] 已依實際組織調整角色權限（系統管理員 / 倉管主管 / 倉庫作業員 / 唯讀）
- [ ] 已刪除或停用不需要的示範資料（WH01 倉庫、MAT001～MAT005 物料）
