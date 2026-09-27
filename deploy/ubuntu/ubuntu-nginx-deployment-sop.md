# WMS v1.0 — Ubuntu + Nginx 部署 SOP

適用於將 WMS 部署到單台 Ubuntu 伺服器，使用 Nginx 同時擔任前端靜態檔案伺服器與
後端 API 的反向代理。

| 項目 | 版本 / 說明 |
|---|---|
| 作業系統 | Ubuntu 22.04 LTS / 24.04 LTS |
| 後端 | ASP.NET Core Web API（.NET 10），以 systemd 常駐於 `127.0.0.1:5080` |
| 前端 | Vue 3 SPA，建置為靜態檔案由 Nginx 直接提供 |
| 資料庫 | PostgreSQL 15 以上 |
| 反向代理 | Nginx 1.18 以上 |

---

## 架構

```text
                    網際網路
                        │
                    443 / 80
                        ↓
        ┌───────────────────────────────┐
        │            Nginx              │
        │                               │
        │  /          → /var/www/wms    │  靜態檔案（Vue SPA）
        │  /api/      → 127.0.0.1:5080  │  反向代理
        │  /swagger/  → 127.0.0.1:5080  │  選用，正式環境建議關閉
        └───────────────┬───────────────┘
                        │
                        ↓
        ┌───────────────────────────────┐
        │   wms-api.service（systemd）   │
        │   /opt/wms/api                │
        └───────────────┬───────────────┘
                        │
                        ↓
                  PostgreSQL
                  127.0.0.1:5432
```

後端只監聽 `127.0.0.1`，不直接對外，所有流量都必須經過 Nginx。

---

## 步驟一：伺服器初始設定

```bash
# 更新套件
sudo apt update && sudo apt upgrade -y

# 設定時區（依實際需求調整）
sudo timedatectl set-timezone Asia/Taipei

# 安裝常用工具
sudo apt install -y curl wget gnupg2 ca-certificates unzip git
```

### 防火牆

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

> 不要開放 5080 與 5432。後端與資料庫只在本機通訊。

### 建立執行用的系統帳號

以專用的無登入帳號執行服務，降低風險：

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin wms
```

---

## 步驟二：安裝 .NET 10 Runtime

正式環境只需要 ASP.NET Core Runtime，不需要 SDK。

```bash
# 加入 Microsoft 套件庫（以 Ubuntu 24.04 為例，22.04 請把 24.04 換成 22.04）
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/ms-prod.deb
sudo dpkg -i /tmp/ms-prod.deb
rm /tmp/ms-prod.deb

sudo apt update
sudo apt install -y aspnetcore-runtime-10.0

# 確認
dotnet --list-runtimes | grep AspNetCore
```

預期輸出應包含 `Microsoft.AspNetCore.App 10.0.x`。

> 若在**建置機**上操作（下面步驟五在本機建置），則需要安裝 `dotnet-sdk-10.0` 而非 runtime。

---

## 步驟三：安裝與設定 PostgreSQL

```bash
sudo apt install -y postgresql postgresql-contrib
sudo systemctl enable --now postgresql
```

### 建立資料庫與使用者

```bash
sudo -u postgres psql <<'SQL'
CREATE USER wms WITH PASSWORD '請改成強式密碼';
CREATE DATABASE wms OWNER wms ENCODING 'UTF8';
GRANT ALL PRIVILEGES ON DATABASE wms TO wms;
SQL
```

驗證連線：

```bash
PGPASSWORD='請改成強式密碼' psql -h 127.0.0.1 -U wms -d wms -c 'SELECT version();'
```

### 確認只監聽本機

```bash
sudo grep -n "^listen_addresses" /etc/postgresql/*/main/postgresql.conf
```

應為 `listen_addresses = 'localhost'`（預設值）。若曾改過，請改回並重啟：

```bash
sudo systemctl restart postgresql
```

---

## 步驟四：取得程式碼

```bash
sudo mkdir -p /opt/wms
sudo chown $USER:$USER /opt/wms

cd /opt/wms
git clone https://github.com/LenShi1989/WMS.git src
cd src
git checkout main
```

---

## 步驟五：建置

建置需要 .NET SDK 與 Node.js。可以在伺服器上建置，也可以在別的機器建置後只上傳產出物。

### 安裝建置工具（若在伺服器上建置）

```bash
sudo apt install -y dotnet-sdk-10.0

# Node.js 20 LTS
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs

node -v && npm -v
```

### 建置後端

```bash
cd /opt/wms/src
dotnet publish src/Wms.Api/Wms.Api.csproj -c Release -o /opt/wms/api
```

### 建置前端

```bash
cd /opt/wms/src/wms-web
npm ci
npm run build
```

產出在 `wms-web/dist`。

> **重要：** 前端的 `VITE_API_BASE_URL` 預設為相對路徑 `/api/v1`，剛好對應 Nginx 的
> 反向代理，因此**不需要**另外設定。只有當 API 位於不同網域時才需要建立 `.env.production`
> 並填入完整網址（同時要調整後端的 CORS 白名單）。

### 部署靜態檔案

```bash
sudo mkdir -p /var/www/wms
sudo rsync -a --delete /opt/wms/src/wms-web/dist/ /var/www/wms/
sudo chown -R www-data:www-data /var/www/wms
```

---

## 步驟六：設定機密與環境變數

**不要**把正式環境的密碼寫進 `appsettings.json`（它在版控中）。改用 systemd 的
EnvironmentFile，ASP.NET Core 會自動以環境變數覆蓋設定檔的同名項目（階層用雙底線 `__`）。

```bash
sudo mkdir -p /etc/wms
sudo tee /etc/wms/api.env > /dev/null <<'ENV'
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5080

# 資料庫連線
ConnectionStrings__Default=Host=127.0.0.1;Port=5432;Database=wms;Username=wms;Password=請改成強式密碼

# JWT 簽章金鑰，請用隨機字串且長度至少 32 字元
Jwt__SecretKey=請改成隨機產生的長字串
Jwt__Issuer=WmsApi
Jwt__Audience=WmsWeb
Jwt__AccessTokenMinutes=60

# 允許的前端來源（同網域部署時仍建議填寫正式網址）
Cors__AllowedOrigins__0=https://wms.example.com

# 首次部署設 true 讓系統自動建表與寫入初始資料；
# 之後若改為由人工套用 Migration，可改成 false
Database__AutoMigrate=true
ENV

# 這個檔案含機密，權限要收緊
sudo chown root:wms /etc/wms/api.env
sudo chmod 640 /etc/wms/api.env
```

產生一組安全的 JWT 金鑰：

```bash
openssl rand -base64 48
```

---

## 步驟七：建立 systemd 服務

```bash
sudo tee /etc/systemd/system/wms-api.service > /dev/null <<'UNIT'
[Unit]
Description=WMS API (ASP.NET Core)
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=notify
WorkingDirectory=/opt/wms/api
ExecStart=/usr/bin/dotnet /opt/wms/api/Wms.Api.dll
EnvironmentFile=/etc/wms/api.env
User=wms
Group=wms
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=wms-api

# 安全強化
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/wms/api

[Install]
WantedBy=multi-user.target
UNIT

sudo chown -R wms:wms /opt/wms/api

sudo systemctl daemon-reload
sudo systemctl enable --now wms-api
sudo systemctl status wms-api --no-pager
```

### 確認後端已啟動

```bash
curl -s http://127.0.0.1:5080/health
```

預期回應：

```json
{"status":"ok","time":"2026-..."}
```

首次啟動時會自動建立 26 張資料表並寫入初始資料（42 項權限、4 種角色、
管理員帳號 `admin`、示範倉庫與物料）。可從日誌確認：

```bash
sudo journalctl -u wms-api -n 50 --no-pager
```

---

## 步驟八：設定 Nginx

```bash
sudo apt install -y nginx
```

```bash
sudo tee /etc/nginx/sites-available/wms > /dev/null <<'CONF'
# 後端上游
upstream wms_api {
    server 127.0.0.1:5080;
    keepalive 32;
}

server {
    listen 80;
    listen [::]:80;
    server_name wms.example.com;

    # 前端靜態檔案
    root /var/www/wms;
    index index.html;

    # 上傳與請求大小上限
    client_max_body_size 20m;

    # gzip 壓縮
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_types text/plain text/css text/xml application/json application/javascript
               application/xml+rss application/atom+xml image/svg+xml;

    # 安全標頭
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # 帶雜湊的靜態資源可長期快取
    location /assets/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    # API 反向代理
    location /api/ {
        proxy_pass http://wms_api;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        proxy_connect_timeout 10s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # 健康檢查
    location /health {
        proxy_pass http://wms_api;
        access_log off;
    }

    # Swagger：正式環境建議關閉，或限制來源 IP
    location /swagger/ {
        # allow 203.0.113.0/24;   # 公司對外 IP
        # deny all;
        return 404;

        # 若確定要開放，把上面的 return 註解掉並啟用以下內容：
        # proxy_pass http://wms_api;
        # proxy_set_header Host $host;
        # proxy_set_header X-Forwarded-Proto $scheme;
    }

    # SPA 路由：找不到實體檔案時一律回 index.html，
    # 否則直接輸入 /inventory 這類網址會 404。
    location / {
        try_files $uri $uri/ /index.html;
    }

    access_log /var/log/nginx/wms.access.log;
    error_log  /var/log/nginx/wms.error.log;
}
CONF

# 啟用站台
sudo ln -sf /etc/nginx/sites-available/wms /etc/nginx/sites-enabled/wms
sudo rm -f /etc/nginx/sites-enabled/default

# 檢查語法後重載
sudo nginx -t && sudo systemctl reload nginx
```

---

## 步驟九：啟用 HTTPS

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d wms.example.com
```

Certbot 會自動改寫上面的設定加入 443 與憑證路徑，並設定自動續約。驗證續約機制：

```bash
sudo certbot renew --dry-run
```

啟用 HTTPS 後，請把 `/etc/wms/api.env` 的 `Cors__AllowedOrigins__0` 改成 `https://` 開頭，
然後重啟後端：

```bash
sudo systemctl restart wms-api
```

---

## 步驟十：驗證部署

```bash
# 1. 健康檢查
curl -s https://wms.example.com/health

# 2. 登入 API
curl -s -X POST https://wms.example.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"a12345678"}' | head -c 200

# 3. 前端首頁
curl -s -o /dev/null -w "%{http_code}\n" https://wms.example.com/

# 4. SPA 深層路由（應回 200 而非 404）
curl -s -o /dev/null -w "%{http_code}\n" https://wms.example.com/inventory
```

接著用瀏覽器開啟 `https://wms.example.com`，以 `admin` / `a12345678` 登入，
確認儀表板、物料、入庫、庫存等頁面可正常操作。

> **登入後第一件事：立刻變更 admin 密碼**（右上角選單 → 個人資料 → 變更密碼）。

---

## 更新版本

```bash
cd /opt/wms/src
git fetch origin
git checkout main
git pull

# 後端
dotnet publish src/Wms.Api/Wms.Api.csproj -c Release -o /opt/wms/api
sudo chown -R wms:wms /opt/wms/api
sudo systemctl restart wms-api

# 前端
cd wms-web
npm ci
npm run build
sudo rsync -a --delete dist/ /var/www/wms/
sudo chown -R www-data:www-data /var/www/wms

# 驗證
curl -s http://127.0.0.1:5080/health
sudo systemctl status wms-api --no-pager
```

若該版本包含資料庫結構變更，`Database__AutoMigrate=true` 時會在啟動時自動套用。
若採人工控管，先備份資料庫，再用 SQL 腳本套用：

```bash
# 在有 SDK 的機器上產生差異腳本
dotnet ef migrations script <上一版Migration> <新版Migration> \
  --project src/Wms.Infrastructure --startup-project src/Wms.Api -o upgrade.sql

# 在伺服器套用
psql -h 127.0.0.1 -U wms -d wms -f upgrade.sql
```

---

## 備份與還原

### 每日自動備份

```bash
sudo mkdir -p /var/backups/wms
sudo tee /usr/local/bin/wms-backup.sh > /dev/null <<'SH'
#!/bin/bash
set -euo pipefail

BACKUP_DIR=/var/backups/wms
STAMP=$(date +%Y%m%d_%H%M%S)

export PGPASSWORD='請改成強式密碼'
pg_dump -h 127.0.0.1 -U wms -d wms -F c -f "${BACKUP_DIR}/wms_${STAMP}.dump"

# 只保留最近 14 天
find "${BACKUP_DIR}" -name 'wms_*.dump' -mtime +14 -delete
SH

sudo chmod 700 /usr/local/bin/wms-backup.sh
sudo chown root:root /usr/local/bin/wms-backup.sh

# 每天凌晨 2 點執行
echo '0 2 * * * root /usr/local/bin/wms-backup.sh' | sudo tee /etc/cron.d/wms-backup
```

### 還原

```bash
sudo systemctl stop wms-api

export PGPASSWORD='請改成強式密碼'
dropdb -h 127.0.0.1 -U postgres wms
createdb -h 127.0.0.1 -U postgres -O wms wms
pg_restore -h 127.0.0.1 -U wms -d wms /var/backups/wms/wms_20260927_020000.dump

sudo systemctl start wms-api
```

---

## 日誌與監控

```bash
# 後端即時日誌
sudo journalctl -u wms-api -f

# 只看錯誤
sudo journalctl -u wms-api -p err --since "1 hour ago"

# Nginx 存取與錯誤
sudo tail -f /var/log/nginx/wms.access.log
sudo tail -f /var/log/nginx/wms.error.log

# 服務狀態
systemctl status wms-api postgresql nginx --no-pager
```

系統內建的 `/health` 可直接接到外部監控（UptimeRobot、Zabbix、Prometheus blackbox 等）。
系統所有重要操作都會寫入 `audit_logs` 資料表，可在「系統管理 → 操作紀錄」查詢。

---

## 疑難排解

| 現象 | 可能原因與處理 |
|---|---|
| `wms-api` 啟動後立刻停止 | `journalctl -u wms-api -n 50`。多半是連線字串錯誤或 `Jwt:SecretKey` 未設定／不足 32 字元 |
| 前端可開，API 都回 502 | 後端沒起來。先 `curl http://127.0.0.1:5080/health` 確認，再看 Nginx `upstream` 位址 |
| 重新整理頁面出現 404 | Nginx 缺少 `try_files $uri $uri/ /index.html`（SPA 路由） |
| 登入回 401 但帳密正確 | 伺服器時間偏移導致 JWT 驗證失敗，執行 `sudo timedatectl set-ntp true` |
| 瀏覽器主控台出現 CORS 錯誤 | `Cors__AllowedOrigins__0` 未填或協定不符（http / https），修改後需重啟後端 |
| 資料表不存在 | 首次啟動時 `Database__AutoMigrate` 為 false。改為 true 重啟，或人工套用 Migration |
| 庫存操作回 409 | 這是正常的業務防護（庫存不足或並行衝突），依畫面訊息處理即可 |

---

## 正式上線前檢查清單

- [ ] `admin` 預設密碼 `a12345678` **已變更**
- [ ] `/etc/wms/api.env` 的資料庫密碼與 `Jwt__SecretKey` 已改為正式值，且權限為 `640`
- [ ] `appsettings.json` 中的開發用密碼未被實際使用（已被環境變數覆蓋）
- [ ] Swagger 已封鎖或限制來源 IP（本 SOP 的 Nginx 設定預設回 404）
- [ ] HTTPS 已啟用，且 `Cors__AllowedOrigins__0` 為 `https://` 網址
- [ ] 防火牆只開放 22 / 80 / 443，未開放 5080 與 5432
- [ ] PostgreSQL 僅監聽 `localhost`
- [ ] 每日備份排程已設定，且**實際測試過還原流程**
- [ ] 已依實際組織調整角色權限（系統管理員 / 倉管主管 / 倉庫作業員 / 唯讀）
- [ ] 已刪除或停用不需要的示範資料（WH01 倉庫、MAT001～MAT005 物料）
