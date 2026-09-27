# WMS v1.0 開發規格書

**文件版本：** v1.0  
**系統名稱：** WMS — Warehouse Management System  
**目標：** 製造業 / 工廠倉儲 / 智慧倉儲  
**Backend：** ASP.NET Core Web API / .NET 10  
**ORM：** EF Core  
**Frontend：** Vue 3 + TypeScript + Vite + Pinia  
**Database：** PostgreSQL 17 / Microsoft SQL Server  
**通訊：** REST API / SignalR / MQTT  
**未來整合：** ERP / MES / WCS / RCS / AGV / AMR

---

# 1. 系統目標

WMS v1.0 必須完成以下基本倉儲流程：

```text
物料主檔
   ↓
建立倉庫 / 儲位
   ↓
入庫
   ↓
收貨
   ↓
上架
   ↓
庫存
   ↓
出庫需求
   ↓
庫存分配
   ↓
揀貨
   ↓
出貨
```

並支援：

- 移庫
- 盤點
- 庫存異動追蹤
- 條碼
- 使用者權限
- 操作紀錄
- Dashboard

---

# 2. v1.0 系統範圍

| 模組 | v1.0 |
|---|---|
| 登入 | ✅ |
| RBAC | ✅ |
| 物料 | ✅ |
| 物料分類 | ✅ |
| 單位 | ✅ |
| 條碼 | ✅ |
| 倉庫 | ✅ |
| Zone | ✅ |
| 儲位 | ✅ |
| 入庫 | ✅ |
| 收貨 | ✅ |
| 上架 | ✅ |
| 庫存 | ✅ |
| 庫存預約 | ✅ |
| 出庫 | ✅ |
| 揀貨 | ✅ |
| 出貨 | ✅ |
| 移庫 | ✅ |
| 盤點 | ✅ |
| 庫存異動 Ledger | ✅ |
| Audit Log | ✅ |
| Dashboard | ✅ |
| REST API | ✅ |
| Swagger | ✅ |

## 2.1 v1.0 暫不包含

```text
Lot / Serial
Advanced QC
FIFO / FEFO
ERP
MES
WCS
RCS
AGV
AMR
AS/RS
RFID
AI Vision
```

這些列入 v1.1 / v2.x，避免第一版過度複雜。

---

# 3. 技術架構

```text
┌─────────────────────────────────────┐
│             Vue 3 Web               │
│                                     │
│ Vue Router / Pinia / Axios          │
│ Tailwind / ECharts                  │
└──────────────────┬──────────────────┘
                   │ HTTPS
                   ↓
┌─────────────────────────────────────┐
│         ASP.NET Core Web API        │
│                                     │
│ Controllers                         │
│ Application Services                │
│ Domain                              │
│ Infrastructure                     │
└──────────────────┬──────────────────┘
                   │
                   ↓
┌─────────────────────────────────────┐
│              EF Core                │
└──────────────────┬──────────────────┘
                   │
          ┌────────┴────────┐
          ↓                 ↓
     PostgreSQL           MSSQL
```

---

# 4. Backend Solution

```text
Wms.sln
│
├── src
│   │
│   ├── Wms.Api
│   ├── Wms.Application
│   ├── Wms.Domain
│   └── Wms.Infrastructure
│
├── tests
│   ├── Wms.UnitTests
│   └── Wms.IntegrationTests
│
└── docs
```

---

# 5. Clean Architecture

## Wms.Domain

只放：

```text
Entities
Enums
ValueObjects
Domain Rules
Domain Events
```

不得依賴：

```text
EF Core
ASP.NET
HTTP
PostgreSQL
```

## Wms.Application

負責：

```text
Use Case
DTO
Command
Query
Service
Validation
Interface
```

例如：

```text
InboundService
OutboundService
InventoryService
PickingService
PutawayService
StocktakeService
```

## Wms.Infrastructure

負責：

```text
EF Core
DbContext
Repository
Database
MQTT
External API
File
Logging
```

## Wms.Api

負責：

```text
Controller
Authentication
Authorization
Middleware
Swagger
Exception Handling
Dependency Injection
```

---

# 6. Frontend 專案

```text
wms-web/
│
├── src/
│   ├── api/
│   ├── assets/
│   ├── components/
│   ├── layouts/
│   ├── router/
│   ├── stores/
│   ├── types/
│   ├── utils/
│   │
│   └── views/
│       ├── auth/
│       ├── dashboard/
│       ├── master/
│       ├── inbound/
│       ├── outbound/
│       ├── inventory/
│       ├── stocktake/
│       └── system/
│
├── App.vue
└── main.ts
```

---

# 7. Database Naming Convention

採：

```text
snake_case
```

例如：

```text
materials
warehouses
warehouse_locations
inbound_orders
inbound_details
inventories
inventory_transactions
```

Primary Key：

```text
id UUID
```

時間：

```text
created_at
updated_at
```

---

# 8. Database 核心 Schema

## 8.1 Users

```text
users
-----------------------------
id
username
display_name
email
password_hash
is_active
created_at
updated_at
```

## 8.2 Roles

```text
roles
-----------------------------
id
name
description
created_at
```

## 8.3 Permissions

```text
permissions
-----------------------------
id
code
name
module
action
```

例如：

```text
MATERIAL_VIEW
MATERIAL_CREATE
MATERIAL_UPDATE
MATERIAL_DELETE

INBOUND_VIEW
INBOUND_CREATE
INBOUND_RECEIVE

INVENTORY_VIEW
INVENTORY_ADJUST
```

## 8.4 UserRoles

```text
user_roles
-----------------------------
user_id
role_id
```

## 8.5 RolePermissions

```text
role_permissions
-----------------------------
role_id
permission_id
```

---

# 9. Materials

```text
materials
-----------------------------
id UUID PK
code VARCHAR(50) UNIQUE
name VARCHAR(200)
specification VARCHAR(500)
category_id UUID
base_uom VARCHAR(20)
is_active BOOLEAN
safety_stock NUMERIC
min_stock NUMERIC
max_stock NUMERIC
created_at
updated_at
```

---

# 10. Material Categories

```text
material_categories
-----------------------------
id
code
name
description
is_active
created_at
updated_at
```

---

# 11. Material Barcodes

```text
material_barcodes
-----------------------------
id
material_id
barcode
barcode_type
is_primary
created_at
```

Unique：

```text
barcode
```

---

# 12. Warehouses

```text
warehouses
-----------------------------
id
code
name
description
is_active
created_at
updated_at
```

---

# 13. Warehouse Zones

```text
warehouse_zones
-----------------------------
id
warehouse_id
code
name
zone_type
is_active
created_at
updated_at
```

Zone：

```text
RECEIVING
STORAGE
PICKING
STAGING
SHIPPING
QC
NG
```

---

# 14. Locations

```text
locations
-----------------------------
id
warehouse_id
zone_id
code
name
location_type
capacity
weight_limit
is_active
created_at
updated_at
```

例如：

```text
WH01-A01-R03-L02-B05
```

---

# 15. Inbound Orders

```text
inbound_orders
-----------------------------
id
order_no
source_type
external_order_no
supplier_code
warehouse_id
status
received_at
created_by
created_at
updated_at
```

Status：

```text
DRAFT
RECEIVING
RECEIVED
PUTAWAY
COMPLETED
CANCELLED
```

---

# 16. Inbound Details

```text
inbound_details
-----------------------------
id
inbound_order_id
material_id
ordered_quantity
received_quantity
putaway_quantity
status
```

---

# 17. Putaway Tasks

```text
putaway_tasks
-----------------------------
id
task_no
inbound_detail_id
material_id
quantity
source_location_id
target_location_id
status
priority
assigned_user_id
created_at
started_at
completed_at
```

Status：

```text
PENDING
ASSIGNED
PROCESSING
COMPLETED
CANCELLED
```

---

# 18. Outbound Orders

```text
outbound_orders
-----------------------------
id
order_no
external_order_no
customer_code
warehouse_id
status
priority
requested_ship_date
created_at
updated_at
```

Status：

```text
DRAFT
ALLOCATED
PICKING
PICKED
SHIPPING
SHIPPED
CANCELLED
```

---

# 19. Outbound Details

```text
outbound_details
-----------------------------
id
outbound_order_id
material_id
requested_quantity
allocated_quantity
picked_quantity
shipped_quantity
status
```

---

# 20. Pick Tasks

```text
pick_tasks
-----------------------------
id
task_no
outbound_detail_id
material_id
source_location_id
quantity
picked_quantity
status
priority
assigned_user_id
created_at
started_at
completed_at
```

---

# 21. Inventory

這是核心 Table。

```text
inventories
-----------------------------
id
warehouse_id
location_id
material_id
quantity
reserved_quantity
available_quantity
status
created_at
updated_at
```

其中：

```text
available_quantity
=
quantity - reserved_quantity
```

實作時由 Service 統一維護，避免前端直接修改。

---

# 22. Inventory Transaction

```text
inventory_transactions
-----------------------------
id
transaction_no
transaction_type
material_id
warehouse_id
from_location_id
to_location_id
quantity
reference_type
reference_id
operator_id
created_at
```

Transaction Type：

```text
RECEIVE
PUTAWAY
PICK
SHIP
TRANSFER
ADJUSTMENT
STOCKTAKE
SCRAP
RESERVE
RELEASE
```

---

# 23. Stocktakes

```text
stocktakes
-----------------------------
id
stocktake_no
warehouse_id
status
started_at
completed_at
created_by
approved_by
created_at
```

---

# 24. Stocktake Details

```text
stocktake_details
-----------------------------
id
stocktake_id
location_id
material_id
system_quantity
counted_quantity
difference_quantity
status
counted_by
counted_at
```

---

# 25. Audit Logs

```text
audit_logs
-----------------------------
id
user_id
action
module
reference_type
reference_id
old_value
new_value
ip_address
created_at
```

---

# 26. Entity 關係

```text
Material
   │
   ├─────────────┐
   ↓             ↓
Inventory      Barcode
   │
   ├── Warehouse
   │
   └── Location
```

入庫：

```text
InboundOrder
     ↓
InboundDetail
     ↓
PutawayTask
     ↓
InventoryTransaction
     ↓
Inventory
```

出庫：

```text
OutboundOrder
     ↓
OutboundDetail
     ↓
PickTask
     ↓
InventoryTransaction
     ↓
Inventory
```

盤點：

```text
Stocktake
     ↓
StocktakeDetail
     ↓
InventoryTransaction
     ↓
Inventory
```

---

# 27. EF Core DbContext

```csharp
public class WmsDbContext : DbContext
{
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();

    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<InboundDetail> InboundDetails => Set<InboundDetail>();

    public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
    public DbSet<OutboundDetail> OutboundDetails => Set<OutboundDetail>();

    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryTransaction> InventoryTransactions
        => Set<InventoryTransaction>();

    public DbSet<PutawayTask> PutawayTasks => Set<PutawayTask>();
    public DbSet<PickTask> PickTasks => Set<PickTask>();

    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();
    public DbSet<StocktakeDetail> StocktakeDetails
        => Set<StocktakeDetail>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
```

---

# 28. Inventory 核心規則

## 規則 1：不可直接修改 Inventory Quantity

```text
❌ Inventory.Quantity += 100
```

必須：

```text
Business Operation
        ↓
InventoryTransaction
        ↓
InventoryService
        ↓
Inventory
```

---

# 29. 入庫交易

例如：

```text
Receive 100
```

產生：

```text
InventoryTransaction
TYPE = RECEIVE
QTY = 100
```

然後：

```text
Inventory += 100
```

---

# 30. 揀貨交易

例如：

```text
庫存 100
Reserved 20
```

可用：

```text
Available = 80
```

揀貨：

```text
Pick 20
```

完成：

```text
Inventory = 80
Reserved = 0
Available = 80
```

---

# 31. 移庫交易

```text
A01
100
```

移 30：

```text
A01 -30
B01 +30
```

產生：

```text
TRANSFER
```

交易，同時記錄：

```text
from_location
to_location
```

---

# 32. API 標準

所有 API：

```text
/api/v1/...
```

例如：

```text
/api/v1/materials
/api/v1/warehouses
/api/v1/locations
/api/v1/inbound-orders
/api/v1/outbound-orders
/api/v1/inventory
/api/v1/stocktakes
```

---

# 33. Material API

```http
GET    /api/v1/materials
GET    /api/v1/materials/{id}
POST   /api/v1/materials
PUT    /api/v1/materials/{id}
DELETE /api/v1/materials/{id}
```

---

# 34. Warehouse API

```http
GET    /api/v1/warehouses
POST   /api/v1/warehouses
GET    /api/v1/warehouses/{id}
PUT    /api/v1/warehouses/{id}
DELETE /api/v1/warehouses/{id}
```

---

# 35. Location API

```http
GET /api/v1/locations
GET /api/v1/locations/{id}
POST /api/v1/locations
PUT /api/v1/locations/{id}
```

支援：

```text
warehouse_id
zone_id
location_type
status
```

篩選。

---

# 36. Inbound API

```http
GET  /api/v1/inbound-orders
GET  /api/v1/inbound-orders/{id}

POST /api/v1/inbound-orders

POST /api/v1/inbound-orders/{id}/receive

POST /api/v1/inbound-orders/{id}/complete
```

---

# 37. Putaway API

```http
GET /api/v1/putaway-tasks

POST /api/v1/putaway-tasks/{id}/assign

POST /api/v1/putaway-tasks/{id}/start

POST /api/v1/putaway-tasks/{id}/complete
```

---

# 38. Inventory API

```http
GET /api/v1/inventory
GET /api/v1/inventory/{id}
```

條件：

```text
material
warehouse
location
status
```

---

# 39. Inventory Transaction API

```http
GET /api/v1/inventory-transactions
GET /api/v1/inventory-transactions/{id}
```

不允許前端任意建立 Transaction。必須由：

```text
Inbound
Outbound
Transfer
Stocktake
```

等業務流程產生。

---

# 40. Outbound API

```http
GET  /api/v1/outbound-orders
GET  /api/v1/outbound-orders/{id}

POST /api/v1/outbound-orders

POST /api/v1/outbound-orders/{id}/allocate
POST /api/v1/outbound-orders/{id}/release
POST /api/v1/outbound-orders/{id}/ship
```

---

# 41. Picking API

```http
GET /api/v1/pick-tasks

POST /api/v1/pick-tasks/{id}/assign
POST /api/v1/pick-tasks/{id}/start
POST /api/v1/pick-tasks/{id}/complete
```

---

# 42. Transfer API

```http
GET  /api/v1/transfers
POST /api/v1/transfers

GET  /api/v1/transfers/{id}

POST /api/v1/transfers/{id}/execute
```

---

# 43. Stocktake API

```http
GET  /api/v1/stocktakes
POST /api/v1/stocktakes

GET /api/v1/stocktakes/{id}

POST /api/v1/stocktakes/{id}/start
POST /api/v1/stocktakes/{id}/count
POST /api/v1/stocktakes/{id}/complete
POST /api/v1/stocktakes/{id}/approve
```

---

# 44. API Response 格式

統一：

```json
{
  "success": true,
  "data": {},
  "message": null,
  "errors": []
}
```

List：

```json
{
  "success": true,
  "data": {
    "items": [],
    "page": 1,
    "pageSize": 20,
    "total": 100
  }
}
```

---

# 45. API Error

```json
{
  "success": false,
  "message": "庫存不足",
  "errors": [
    {
      "code": "INSUFFICIENT_STOCK",
      "message": "MAT001 可用庫存只有 30"
    }
  ]
}
```

HTTP：

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
500 Internal Server Error
```

---

# 46. Vue Route

```text
/login

/dashboard

/master/materials
/master/categories
/master/warehouses
/master/locations
/master/barcodes

/inbound/orders
/inbound/orders/:id
/inbound/putaway

/outbound/orders
/outbound/orders/:id
/outbound/picking
/outbound/shipping

/inventory
/inventory/transactions
/inventory/transfer

/stocktake

/system/users
/system/roles
/system/audit-logs
```

---

# 47. Vue Pinia Store

```text
stores/
│
├── auth.ts
├── material.ts
├── warehouse.ts
├── location.ts
├── inbound.ts
├── outbound.ts
├── inventory.ts
├── picking.ts
├── putaway.ts
└── stocktake.ts
```

---

# 48. PDA / Mobile 模式

第一版可以先做 Responsive Web。

```text
┌───────────────────┐
│       WMS PDA     │
├───────────────────┤
│ [ 掃描條碼 ]      │
├───────────────────┤
│ 任務：PK000123    │
│                   │
│ MAT001            │
│ 數量：20          │
│                   │
│ A01-02-03         │
│                   │
│ [確認]            │
└───────────────────┘
```

未來再考慮原生 Android。

---

# 49. Dashboard API

```http
GET /api/v1/dashboard/summary
```

Response：

```json
{
  "inboundToday": 1280,
  "outboundToday": 950,
  "inventoryTotal": 85230,
  "pendingPutaway": 38,
  "pendingPicking": 120,
  "pendingStocktake": 12
}
```

---

# 50. Authentication

採：

```text
JWT Bearer
```

登入：

```http
POST /api/v1/auth/login
```

Response：

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 3600
}
```

---

# 51. RBAC

Controller：

```csharp
[Authorize(Policy = "Inventory.View")]
```

例如：

```text
Inventory.View
Inventory.Adjust
Inventory.Transfer

Inbound.View
Inbound.Receive

Outbound.View
Outbound.Pick
Outbound.Ship
```

---

# 52. Transaction / Concurrency

WMS 容易遇到並行庫存操作：

```text
A 人看到庫存 100
B 人也看到庫存 100

A 揀 60
B 揀 60
```

因此 Inventory 必須支援：

```text
Optimistic Concurrency
```

例如：

```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = [];
```

PostgreSQL 則使用適合 PostgreSQL 的 concurrency token 設計。

---

# 53. 防止超賣

Inventory Service 必須：

```text
Transaction
 ↓
Lock / Concurrency Check
 ↓
Available >= Required
 ↓
Reserve
```

例如：

```text
Available = 50

Order A = 40
Order B = 30
```

不能讓兩張訂單同時成功。

---

# 54. 入庫流程

```text
                    Inbound
                       │
                       ↓
                  Create Order
                       │
                       ↓
                    Receive
                       │
                       ↓
                 Receive Confirm
                       │
                       ↓
                  Create Task
                       │
                       ↓
                   Putaway
                       │
                       ↓
             Inventory Transaction
                       │
                       ↓
                   Inventory
```

---

# 55. 出庫流程

```text
                  Outbound
                     │
                     ↓
                Create Order
                     │
                     ↓
                 Allocate
                     │
                     ↓
                 Pick Task
                     │
                     ↓
                  Picking
                     │
                     ↓
               Pick Complete
                     │
                     ↓
                  Shipping
                     │
                     ↓
             Inventory Transaction
```

---

# 56. 移庫流程

```text
Transfer Order
      ↓
Create Task
      ↓
Scan Source
      ↓
Scan Material
      ↓
Input Quantity
      ↓
Scan Destination
      ↓
Confirm
      ↓
Inventory Transaction
```

---

# 57. 盤點流程

```text
Create Stocktake
       ↓
Generate Details
       ↓
Freeze / Snapshot
       ↓
Count
       ↓
Compare
       ↓
Difference?
   ┌───┴────┐
   No       Yes
   │         │
   ↓         ↓
Complete   Recount
             ↓
          Approve
             ↓
        Adjustment
```

---

# 58. v1.0 Frontend Menu

```text
WMS
│
├── Dashboard
│
├── 基礎資料
│   ├── 物料
│   ├── 物料分類
│   ├── 倉庫
│   ├── 儲位
│   └── 條碼
│
├── 入庫管理
│   ├── 入庫單
│   ├── 收貨
│   └── 上架
│
├── 出庫管理
│   ├── 出庫單
│   ├── 揀貨
│   └── 出貨
│
├── 庫存管理
│   ├── 即時庫存
│   ├── 庫存異動
│   └── 移庫
│
├── 盤點管理
│   └── 盤點
│
└── 系統管理
    ├── 使用者
    ├── 角色
    ├── 權限
    └── 操作紀錄
```

---

# 59. 開發 Phase

## Phase 1A — 基礎架構

```text
.NET 10
EF Core
PostgreSQL
Swagger
JWT
Vue 3
Pinia
Axios
```

完成：

```text
Login
Layout
RBAC
Database
Migration
```

## Phase 1B — Master Data

```text
Material
Category
UOM
Warehouse
Zone
Location
Barcode
```

## Phase 1C — Inventory Core

```text
Inventory
InventoryTransaction
Reservation
Concurrency
```

## Phase 1D — Inbound

```text
Inbound
Receiving
Putaway
```

## Phase 1E — Outbound

```text
Outbound
Allocation
Picking
Shipping
```

## Phase 1F — Warehouse Operation

```text
Transfer
Stocktake
AuditLog
Dashboard
```

---

# 60. Phase 2

```text
Lot
Serial
Expiry
FIFO
FEFO
QC
HOLD
NG
Scrap
ERP
MES
MQTT
SignalR
```

---

# 61. Phase 3

```text
WCS
RCS
AGV
AMR
AS/RS
Conveyor
PLC
RFID
Vision
AI
```

---

# 62. Git Branch Strategy

```text
main
 │
 └── develop
      │
      ├── feature/auth
      ├── feature/material
      ├── feature/inventory
      ├── feature/inbound
      ├── feature/outbound
      ├── feature/stocktake
      └── feature/dashboard
```

Commit 建議：

```text
feat: add material management
feat: add inventory transaction
feat: add inbound receiving
feat: add putaway task
feat: add outbound picking
fix: prevent inventory over allocation
```

---

# 63. v1.0 驗收標準

## 基礎資料

- [ ] 可以建立物料
- [ ] 可以建立倉庫
- [ ] 可以建立儲位
- [ ] 可以設定條碼
- [ ] 可以停用物料

## 入庫

- [ ] 建立入庫單
- [ ] 收貨
- [ ] 收貨數量驗證
- [ ] 產生上架任務
- [ ] 完成上架
- [ ] Inventory 增加
- [ ] 產生 Transaction

## 出庫

- [ ] 建立出庫單
- [ ] 庫存分配
- [ ] 防止超額分配
- [ ] 建立 Pick Task
- [ ] 完成揀貨
- [ ] 出貨
- [ ] Inventory 減少

## 移庫

- [ ] 建立移庫
- [ ] Source 驗證
- [ ] Destination 驗證
- [ ] 完成移庫
- [ ] 庫存正確轉移

## 盤點

- [ ] 建立盤點
- [ ] 系統數量 Snapshot
- [ ] 輸入盤點數
- [ ] 計算差異
- [ ] 複盤
- [ ] 調整庫存
- [ ] 建立 Transaction

---

# 64. WMS 設計原則

1. Inventory 不允許前端直接修改。
2. 所有庫存異動必須有 Transaction。
3. Order 與 Task 分離。
4. Task 與 Inventory Transaction 分離。
5. Location 必須獨立 Entity。
6. Reserved Quantity 與 Physical Quantity 分開。
7. 所有重要操作必須 Audit。
8. API 不直接暴露 EF Entity。
9. Controller 不放 Business Logic。
10. WMS 不直接控制 AGV / PLC。

其中第 10 條：

```text
WMS
 │
 │ "我要搬貨"
 ↓
RCS / WCS
 │
 │ "怎麼搬"
 ↓
AGV / PLC
```

---

# 65. 最終架構

```text
                         ┌───────────────┐
                         │      ERP      │
                         └───────┬───────┘
                                 │
                         REST / MQ / API
                                 │
                                 ↓
┌────────────────────────────────────────────────────┐
│                       WMS                          │
│                                                    │
│  Master Data                                       │
│       │                                            │
│       ↓                                            │
│  ┌─────────┐      ┌─────────┐      ┌──────────┐  │
│  │ Inbound │ ───→ │Inventory│ ←─── │ Outbound │  │
│  └────┬────┘      └────┬────┘      └────┬─────┘  │
│       │                 │                 │        │
│   Putaway          Transaction         Picking     │
│                         │                 │        │
│                         ↓                 ↓        │
│                    Stocktake           Shipping    │
│                                                    │
│                Warehouse Task Engine               │
└────────────────────────┬───────────────────────────┘
                         │
                ┌────────┴────────┐
                │                 │
               WCS               RCS
                │                 │
          AS/RS / PLC        AGV / AMR
```

---

# 66. 正式開發順序

```text
01  建立 Git Repository
        ↓
02  建立 .NET 10 Solution
        ↓
03  建立 Vue 3 + Vite + TypeScript
        ↓
04  建立 PostgreSQL
        ↓
05  EF Core DbContext
        ↓
06  User / Role / Permission
        ↓
07  Material / Category / UOM
        ↓
08  Warehouse / Zone / Location
        ↓
09  Inventory
        ↓
10  InventoryTransaction
        ↓
11  Inbound
        ↓
12  Putaway
        ↓
13  Outbound
        ↓
14  Picking
        ↓
15  Transfer
        ↓
16  Stocktake
        ↓
17  AuditLog
        ↓
18  Dashboard
        ↓
19  API Integration
        ↓
20  Docker / CI/CD
```

## 第一個 Sprint

先把以下流程完整跑通：

```text
Material
   ↓
Warehouse
   ↓
Location
   ↓
Inbound
   ↓
Putaway
   ↓
Inventory
   ↓
InventoryTransaction
```

驗證：

> MAT001 收貨 100 個 → 上架到 A01-01-01 → 查詢庫存必須看到 100 → 查詢 Transaction 必須看到完整異動。

第二條：

```text
Inventory 100
      ↓
Outbound 30
      ↓
Allocate 30
      ↓
Pick 30
      ↓
Ship 30
      ↓
Inventory 70
```

只要這兩條流程在 **API + DB + Vue** 三層完整跑通，WMS v1.0 核心架構即成立。
