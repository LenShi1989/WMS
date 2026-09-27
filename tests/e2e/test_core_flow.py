"""WMS v1.0 核心流程煙霧測試：入庫→上架→庫存→出庫→揀貨→出貨。"""
import json
import sys
import urllib.request
import urllib.error

sys.stdout.reconfigure(encoding='utf-8')

BASE = "http://localhost:5173/api/v1"  # 走前端 Vite proxy，等同瀏覽器實際呼叫路徑
TOKEN = None
FAILED = []


def call(method, path, body=None, expect_ok=True):
    url = BASE + path
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if TOKEN:
        req.add_header("Authorization", "Bearer " + TOKEN)
    try:
        with urllib.request.urlopen(req) as resp:
            payload = json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        payload = json.loads(e.read().decode())
    if expect_ok and not payload.get("success"):
        raise RuntimeError(f"{method} {path} -> {payload}")
    return payload


def check(label, actual, expected):
    ok = actual == expected
    print(f"  [{'PASS' if ok else 'FAIL'}] {label}: 實際={actual} 預期={expected}")
    if not ok:
        FAILED.append(label)


print("=== 1. 登入 ===")
login = call("POST", "/auth/login", {"username": "admin", "password": "a12345678"})
TOKEN = login["data"]["accessToken"]
print(f"  登入成功：{login['data']['user']['displayName']}，權限 {len(login['data']['user']['permissions'])} 項")

print("=== 2. 讀取主檔 ===")
materials = call("GET", "/materials?pageSize=10")["data"]["items"]
mat = next(m for m in materials if m["code"] == "MAT001")
wh = call("GET", "/warehouses")["data"]["items"][0]
locations = call("GET", f"/locations?warehouseId={wh['id']}&pageSize=50")["data"]["items"]
storage = next(l for l in locations if l["code"] == "WH01-A01-R01-L01")
storage2 = next(l for l in locations if l["code"] == "WH01-A02-R01-L01")
print(f"  物料 {mat['code']} / 倉庫 {wh['code']} / 儲位 {storage['code']}")

print("=== 3. 建立入庫單（MAT001 x 100）===")
inbound = call("POST", "/inbound-orders", {
    "warehouseId": wh["id"],
    "supplierCode": "SUP001",
    "supplierName": "示範供應商",
    "details": [{"materialId": mat["id"], "orderedQuantity": 100}],
})["data"]
print(f"  入庫單號 {inbound['orderNo']}，狀態 {inbound['status']}")
check("入庫單初始狀態", inbound["status"], "DRAFT")

print("=== 4. 收貨 100 ===")
received = call("POST", f"/inbound-orders/{inbound['id']}/receive", {
    "lines": [{"inboundDetailId": inbound["details"][0]["id"], "receivedQuantity": 100}]
})["data"]
check("收貨後單據狀態", received["status"], "RECEIVED")
check("已收數量", received["details"][0]["receivedQuantity"], 100)

print("=== 5. 收貨數量驗證（超收應被擋下）===")
over = call("POST", f"/inbound-orders/{inbound['id']}/receive", {
    "lines": [{"inboundDetailId": inbound["details"][0]["id"], "receivedQuantity": 10}]
}, expect_ok=False)
check("超收被拒絕", over["success"], False)
check("超收錯誤碼", over["errors"][0]["code"], "OVER_RECEIVE")

print("=== 6. 上架任務 ===")
tasks = call("GET", f"/putaway-tasks?status=PENDING")["data"]["items"]
task = next(t for t in tasks if t["inboundOrderNo"] == inbound["orderNo"])
print(f"  任務 {task['taskNo']}，數量 {task['quantity']}，來源 {task['sourceLocationCode']}")
done = call("POST", f"/putaway-tasks/{task['id']}/complete",
            {"targetLocationId": storage["id"]})["data"]
check("上架任務狀態", done["status"], "COMPLETED")
check("上架目標儲位", done["targetLocationCode"], storage["code"])

print("=== 7. 查詢庫存（應為 100）===")
inv = call("GET", f"/inventory?materialId={mat['id']}&onlyInStock=true")["data"]["items"]
total = sum(i["quantity"] for i in inv)
check("MAT001 總庫存", total, 100)
at_storage = next(i for i in inv if i["locationCode"] == storage["code"])
check(f"{storage['code']} 庫存", at_storage["quantity"], 100)
check(f"{storage['code']} 可用量", at_storage["availableQuantity"], 100)

print("=== 8. 查詢異動紀錄 ===")
txs = call("GET", f"/inventory-transactions?materialId={mat['id']}&pageSize=50")["data"]["items"]
types = sorted({t["transactionType"] for t in txs})
print(f"  異動筆數 {len(txs)}，類型 {types}")
check("有 RECEIVE 異動", "RECEIVE" in types, True)
check("有 PUTAWAY 異動", "PUTAWAY" in types, True)

print("=== 9. 入庫單結案 ===")
completed = call("POST", f"/inbound-orders/{inbound['id']}/complete")["data"]
check("入庫單結案狀態", completed["status"], "COMPLETED")

print("=== 10. 建立出庫單（MAT001 x 30）===")
outbound = call("POST", "/outbound-orders", {
    "warehouseId": wh["id"],
    "customerCode": "CUS001",
    "customerName": "示範客戶",
    "details": [{"materialId": mat["id"], "requestedQuantity": 30}],
})["data"]
print(f"  出庫單號 {outbound['orderNo']}")

print("=== 11. 庫存分配 ===")
alloc = call("POST", f"/outbound-orders/{outbound['id']}/allocate")["data"]
check("分配完整性", alloc["fullyAllocated"], True)
check("分配數量", alloc["lines"][0]["allocatedQuantity"], 30)
check("產生揀貨任務數", alloc["createdPickTaskCount"], 1)

inv_after_alloc = call("GET", f"/inventory?materialId={mat['id']}&onlyInStock=true")["data"]["items"][0]
check("分配後預留量", inv_after_alloc["reservedQuantity"], 30)
check("分配後可用量", inv_after_alloc["availableQuantity"], 70)

print("=== 12. 防止超額分配（再開一張 80 的單）===")
big = call("POST", "/outbound-orders", {
    "warehouseId": wh["id"],
    "details": [{"materialId": mat["id"], "requestedQuantity": 80}],
})["data"]
big_alloc = call("POST", f"/outbound-orders/{big['id']}/allocate")["data"]
check("第二張單僅能分配剩餘可用量", big_alloc["lines"][0]["allocatedQuantity"], 70)
check("第二張單短缺量", big_alloc["lines"][0]["shortageQuantity"], 10)
check("第二張單未完整分配", big_alloc["fullyAllocated"], False)
call("POST", f"/outbound-orders/{big['id']}/release")
print("  已釋放第二張單的預留")

print("=== 13. 揀貨 ===")
picks = call("GET", f"/pick-tasks?outboundOrderId={outbound['id']}")["data"]["items"]
pick = picks[0]
print(f"  任務 {pick['taskNo']}，來源 {pick['sourceLocationCode']}，數量 {pick['quantity']}")
picked = call("POST", f"/pick-tasks/{pick['id']}/complete", {})["data"]
check("揀貨任務狀態", picked["status"], "COMPLETED")

inv_after_pick = call("GET", f"/inventory?materialId={mat['id']}&onlyInStock=true")["data"]["items"][0]
check("揀貨後庫存", inv_after_pick["quantity"], 70)
check("揀貨後預留量", inv_after_pick["reservedQuantity"], 0)
check("揀貨後可用量", inv_after_pick["availableQuantity"], 70)

print("=== 14. 出貨 ===")
shipped = call("POST", f"/outbound-orders/{outbound['id']}/ship", {"remark": "煙霧測試出貨"})["data"]
check("出庫單狀態", shipped["status"], "SHIPPED")
check("出貨數量", shipped["details"][0]["shippedQuantity"], 30)

print("=== 15. 移庫（A01 → A02，30 個）===")
transfer = call("POST", "/transfers", {
    "materialId": mat["id"],
    "fromLocationId": storage["id"],
    "toLocationId": storage2["id"],
    "quantity": 30,
    "reason": "煙霧測試移庫",
    "executeImmediately": True,
})["data"]
check("移庫狀態", transfer["status"], "COMPLETED")
inv_all = call("GET", f"/inventory?materialId={mat['id']}&onlyInStock=true")["data"]["items"]
by_loc = {i["locationCode"]: i["quantity"] for i in inv_all}
check(f"{storage['code']} 移庫後", by_loc.get(storage["code"]), 40)
check(f"{storage2['code']} 移庫後", by_loc.get(storage2["code"]), 30)

print("=== 16. 盤點（A02 實盤 28，短少 2）===")
st = call("POST", "/stocktakes", {"warehouseId": wh["id"], "remark": "煙霧測試盤點"})["data"]
st = call("POST", f"/stocktakes/{st['id']}/start")["data"]
lines = []
for d in st["details"]:
    counted = 28 if d["locationCode"] == storage2["code"] else d["systemQuantity"]
    lines.append({"detailId": d["id"], "countedQuantity": counted})
st = call("POST", f"/stocktakes/{st['id']}/count", {"lines": lines})["data"]
check("盤點差異筆數", st["differenceCount"], 1)
st = call("POST", f"/stocktakes/{st['id']}/complete")["data"]
check("盤點待核准", st["status"], "PENDING_APPROVAL")
st = call("POST", f"/stocktakes/{st['id']}/approve")["data"]
check("盤點已核准", st["status"], "APPROVED")

inv_final = call("GET", f"/inventory?materialId={mat['id']}&onlyInStock=true")["data"]["items"]
final_by_loc = {i["locationCode"]: i["quantity"] for i in inv_final}
check(f"{storage2['code']} 盤後調整", final_by_loc.get(storage2["code"]), 28)

print("=== 17. 庫存異動 Ledger 完整性 ===")
txs = call("GET", f"/inventory-transactions?materialId={mat['id']}&pageSize=100")["data"]["items"]
types = sorted({t["transactionType"] for t in txs})
print(f"  共 {len(txs)} 筆異動，類型：{types}")
for expected in ["RECEIVE", "PUTAWAY", "RESERVE", "PICK", "SHIP", "TRANSFER", "STOCKTAKE", "RELEASE"]:
    check(f"Ledger 含 {expected}", expected in types, True)

print("=== 18. Dashboard ===")
summary = call("GET", "/dashboard/summary")["data"]
print(f"  今日入庫 {summary['inboundToday']} / 今日出庫 {summary['outboundToday']} / 庫存總量 {summary['inventoryTotal']}")
check("今日入庫量", summary["inboundToday"], 100)
check("今日出庫量", summary["outboundToday"], 30)

print("=== 19. 操作紀錄 ===")
logs = call("GET", "/audit-logs?pageSize=5")["data"]
print(f"  共 {logs['total']} 筆操作紀錄，最新：{logs['items'][0]['module']}/{logs['items'][0]['action']}")
check("有操作紀錄", logs["total"] > 0, True)

print("=== 20. 未授權存取 ===")
saved, TOKEN = TOKEN, None
try:
    urllib.request.urlopen(urllib.request.Request(BASE + "/materials"))
    check("未帶 Token 應被拒絕", "allowed", "401")
except urllib.error.HTTPError as e:
    check("未帶 Token 回應碼", e.code, 401)
TOKEN = saved

print()
if FAILED:
    print(f"XXX {len(FAILED)} 項檢查失敗：{FAILED}")
    sys.exit(1)
print(">>> 全部檢查通過：WMS v1.0 核心流程在 API + DB 兩層完整跑通。")
