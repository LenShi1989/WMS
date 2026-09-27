# WMS v1.0 — 倉儲物料管理系統

依《WMS_v1.0_開發規格書.md》實作的倉儲管理系統，涵蓋從物料主檔、入庫收貨、上架、
庫存、出庫分配、揀貨、出貨，到移庫、盤點、庫存異動追蹤與操作紀錄的完整流程。

| 項目 | 技術 |
|---|---|
| 後端 | ASP.NET Core Web API / .NET 10 |
| ORM | EF Core 10 + Npgsql |
| 前端 | Vue 3 + TypeScript + Vite + Pinia + Vue Router |
| 樣式 | Tailwind CSS v4（TailAdmin 風格後台版面） |
| 圖表 | ECharts |
| 資料庫 | PostgreSQL |
| 認證 | JWT Bearer（Access Token + Refresh Token） |
| API 文件 | Swagger / OpenAPI |

---

## 快速開始

### 1. 環境需求

- .NET SDK 10.0+
- Node.js 20+
- PostgreSQL 15+（開發環境使用 18.6 驗證）

### 2. 設定資料庫連線

預設連線字串寫在 `src/Wms.Api/appsettings.json`：

```json
"ConnectionStrings": {
  "Default": "Host=127.0.0.1;Port=5432;Database=wms;Username=postgres;Password=你的密碼"
}
```

> 正式環境請改用環境變數或 User Secrets，不要把密碼留在版控中。

### 3. 啟動後端

```bash
dotnet run --project src/Wms.Api --urls http://localhost:5080
```

啟動時會自動建立資料庫、套用 Migration，並寫入初始資料（權限、角色、管理員帳號、
示範倉庫與物料）。可用 `appsettings.json` 的 `Database:AutoMigrate` 關閉這個行為。

- API：<http://localhost:5080/api/v1>
- Swagger：<http://localhost:5080/swagger>
- 健康檢查：<http://localhost:5080/health>

### 4. 啟動前端

```bash
cd wms-web
cp .env.example .env
npm install
npm run dev
```

前端位於 <http://localhost:5173>，開發模式透過 Vite proxy 轉發 `/api` 到後端，
因此瀏覽器端不會遇到 CORS 問題。

### 5. 登入

| 帳號 | 密碼 |
|---|---|
| `admin` | `a12345678` |

---

## 專案結構

```text
WMS/
├── Wms.slnx
├── src/
│   ├── Wms.Domain          # Entity、Enum，不依賴任何框架
│   ├── Wms.Application     # Use Case、DTO、Service 介面與實作、業務規則
│   ├── Wms.Infrastructure  # EF Core、DbContext、Migration、JWT、密碼雜湊、稽核
│   └── Wms.Api             # Controller、認證授權、Middleware、Swagger、DI
└── wms-web/
    └── src/
        ├── api/            # Axios client 與各模組 API
        ├── components/     # DataTable、Modal、StatusBadge 等共用元件
        ├── composables/    # useDataTable（列表分頁共用邏輯）
        ├── layouts/        # AdminLayout（側邊欄 + 頂欄）
        ├── router/         # 路由與選單定義
        ├── stores/         # Pinia（auth / app / master）
        ├── types/          # 對應後端 DTO 的型別
        ├── utils/          # 格式化、狀態標籤、權限碼
        └── views/          # 各功能頁面
```

### 分層原則

- `Wms.Domain` 不依賴 EF Core、ASP.NET 或任何資料庫套件。
- `Wms.Application` 透過 `IWmsDbContext` 介面存取資料，不綁定具體 DbContext。
- Controller 不含業務邏輯，只負責呼叫 Service 與包裝回應。
- API 一律回傳 DTO，不直接暴露 EF Entity。

---

## 庫存核心設計

### 唯一的庫存異動入口

`Inventory` 的數量只能經由 `IInventoryEngine` 變更，而且每一次變更都會同時產生一筆
`InventoryTransaction`。任何服務都不允許直接寫 `Inventory.Quantity`。

```text
業務操作（收貨 / 上架 / 揀貨 / 移庫 / 盤點 / 調整）
        ↓
   InventoryEngine
        ↓
InventoryTransaction（流水） + Inventory（結存）
```

### 三層防超賣

| 層級 | 機制 |
|---|---|
| 應用層 | 分配前檢查 `可用量 = 數量 − 預留量`，不足即拒絕 |
| 交易層 | 分配、揀貨、移庫、盤點核准都包在資料庫交易內 |
| 資料庫層 | `xmin` 樂觀鎖 + CHECK 限制（數量 ≥ 0、預留 ≥ 0、預留 ≤ 數量） |

即使應用層出現漏洞，資料庫的 CHECK 限制仍會讓超賣的交易失敗。

### 可用量欄位

`available_quantity` 是 PostgreSQL 的 stored generated column：

```sql
available_quantity numeric(18,6) GENERATED ALWAYS AS (quantity - reserved_quantity) STORED
```

資料庫保證它永遠與實際數量一致，不需要程式維護。

---

## 業務流程

### 入庫

```text
建立入庫單(DRAFT) → 收貨(RECEIVING/RECEIVED) → 自動產生上架任務
      → 完成上架(PUTAWAY) → 結案(COMPLETED)
```

收貨時庫存先進入「收貨區」儲位（`RECEIVE` 異動），上架時再移到儲存儲位
（`PUTAWAY` 異動）。倉庫若尚未建立收貨區，系統會自動補一個收貨暫存儲位。

### 出庫

```text
建立出庫單(DRAFT) → 庫存分配(ALLOCATED) → 揀貨(PICKING/PICKED) → 出貨(SHIPPED)
```

- 分配：預留庫存（`RESERVE`）並依儲位產生揀貨任務。
- 揀貨：扣除庫存並同步消耗預留量（`PICK`）；短揀的差額會自動釋放（`RELEASE`）。
- 出貨：留下 `SHIP` 憑證並結案（庫存已在揀貨時扣除）。

### 盤點

```text
建立(Snapshot 帳面數量) → 開始 → 輸入實盤數 → 計算差異
      → 複盤(選填) → 結束盤點 → 核准 → 依差異調整庫存
```

建立盤點單時會 Snapshot 當下庫存作為帳面基準，之後的異動不影響盤點結果。

---

## 權限設計

權限碼統一為 `MODULE_ACTION` 格式（例如 `MATERIAL_VIEW`、`INBOUND_RECEIVE`），
後端 `Wms.Application.Common.Permissions` 與前端 `src/utils/permissions.ts`
使用同一組字串，共 42 項。

Controller 直接以權限碼當作 Policy 名稱：

```csharp
[Authorize(Policy = Permissions.InventoryAdjust)]
```

`PermissionPolicyProvider` 會依名稱動態產生 Policy，不需要逐一註冊。
登入時權限會寫入 JWT 的 `permission` claim，授權檢查不必每次查資料庫。

內建四種角色：**系統管理員**（全部權限）、**倉管主管**、**倉庫作業員**、**唯讀使用者**。

---

## API 規範

所有端點都在 `/api/v1` 之下，統一回應格式：

```json
{ "success": true, "data": { }, "message": null, "errors": [] }
```

分頁結果：

```json
{ "success": true, "data": { "items": [], "page": 1, "pageSize": 20, "total": 100 } }
```

錯誤：

```json
{
  "success": false,
  "message": "可用庫存不足：可用 30，需求 50",
  "errors": [{ "code": "INSUFFICIENT_STOCK", "message": "..." }]
}
```

| HTTP | 使用時機 |
|---|---|
| 400 | 輸入驗證失敗、業務規則不符 |
| 401 | 未登入或 Token 失效 |
| 403 | 權限不足 |
| 404 | 資料不存在 |
| 409 | 狀態衝突、庫存不足、並行衝突 |
| 500 | 未預期錯誤 |

---

## 資料庫

命名一律 `snake_case`，主鍵為 `uuid`，時間欄位為 `timestamptz`，
列舉以字串儲存，數量統一 `numeric(18,6)`。共 25 張業務資料表。

常用指令：

```bash
# 新增 Migration
dotnet ef migrations add <名稱> --project src/Wms.Infrastructure --startup-project src/Wms.Api --output-dir Persistence/Migrations

# 套用 Migration
dotnet ef database update --project src/Wms.Infrastructure --startup-project src/Wms.Api

# 匯出 SQL 腳本
dotnet ef migrations script --project src/Wms.Infrastructure --startup-project src/Wms.Api -o wms.sql
```

---

## 建置

```bash
# 後端
dotnet build

# 前端（含型別檢查）
cd wms-web && npm run build
```

---

## v1.0 範圍

**已包含**：登入、RBAC、物料 / 分類 / 單位 / 條碼、倉庫 / 儲區 / 儲位、入庫 / 收貨 /
上架、庫存 / 預留、出庫 / 分配 / 揀貨 / 出貨、移庫、盤點、庫存異動 Ledger、
操作紀錄、Dashboard、REST API、Swagger。

**不包含**（列入 v1.1 / v2.x）：Lot / Serial、效期、FIFO / FEFO、進階 QC、
ERP / MES / WCS / RCS 整合、AGV / AMR / AS-RS、RFID、AI Vision。
