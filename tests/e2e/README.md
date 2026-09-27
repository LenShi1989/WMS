# 端到端驗收腳本

對照規格書 §63「v1.0 驗收標準」的可執行版本，直接打真實的 API 與資料庫。
只用 Python 標準函式庫，不需要安裝任何套件。

## 前置條件

1. 後端已啟動：`dotnet run --project src/Wms.Api --urls http://localhost:5080`
2. 前端開發伺服器已啟動：`cd wms-web && npm run dev`
3. 資料庫已套用 Migration 與初始資料（後端啟動時會自動完成）

腳本預設打 `http://localhost:5173/api/v1`，也就是走前端的 Vite proxy，
等同瀏覽器實際呼叫的路徑；若只想測後端，把檔案裡的 `BASE` 改成 `http://localhost:5080/api/v1`。

## 腳本

| 檔案 | 驗證內容 |
|---|---|
| `test_core_flow.py` | 入庫→收貨→上架→庫存→出庫→分配→揀貨→出貨→移庫→盤點的完整流程，共 40 項檢查 |
| `test_concurrency.py` | 併發分配不得超賣（規格書 §52 / §53） |
| `test_api_contract.py` | 前端 `src/api/index.ts` 宣告的每個端點在後端都存在 |

## 執行

```bash
python tests/e2e/test_core_flow.py
python tests/e2e/test_concurrency.py
python tests/e2e/test_api_contract.py
```

全部通過時結束碼為 0，有任何檢查失敗則為 1，可直接接進 CI。

## 注意

`test_core_flow.py` 會建立入庫單、出庫單、移庫單與盤點單，並且是**不可逆**的，
因此每次重跑前請先清空交易資料：

```sql
TRUNCATE inventory_transactions, inventories, putaway_tasks, pick_tasks,
         inbound_details, inbound_orders, outbound_details, outbound_orders,
         transfer_orders, stocktake_details, stocktakes, audit_logs, number_sequences
  RESTART IDENTITY CASCADE;
```

主檔（物料、倉庫、儲位、使用者、權限）不受影響。
