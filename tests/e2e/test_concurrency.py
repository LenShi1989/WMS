"""
併發防超賣驗證（對應規格書 §52 Concurrency、§53 防止超賣）。

情境：可用庫存 50，同時送出 8 張各要 40 的出庫單並行分配。
正確行為：分配總量不得超過 50，且庫存的 reserved_quantity 不得大於 quantity。
"""
import json
import sys
import threading
import urllib.error
import urllib.request

sys.stdout.reconfigure(encoding='utf-8')

BASE = "http://localhost:5173/api/v1"
TOKEN = None
FAILED = []

ORDER_COUNT = 8
ORDER_QTY = 40
STOCK_QTY = 50


def call(method, path, body=None, token=None):
    req = urllib.request.Request(
        BASE + path,
        data=json.dumps(body).encode() if body is not None else None,
        method=method,
    )
    req.add_header("Content-Type", "application/json")
    if token or TOKEN:
        req.add_header("Authorization", "Bearer " + (token or TOKEN))
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        return json.loads(e.read().decode())


def check(label, actual, expected):
    ok = actual == expected
    print(f"  [{'PASS' if ok else 'FAIL'}] {label}: 實際={actual} 預期={expected}")
    if not ok:
        FAILED.append(label)


TOKEN = call("POST", "/auth/login", {"username": "admin", "password": "a12345678"})["data"]["accessToken"]

wh = call("GET", "/warehouses")["data"]["items"][0]
mat = next(m for m in call("GET", "/materials?pageSize=20")["data"]["items"] if m["code"] == "MAT002")
loc = next(
    l for l in call("GET", f"/locations?warehouseId={wh['id']}&pageSize=50")["data"]["items"]
    if l["code"] == "WH01-A02-R01-L02"
)

print(f"=== 準備：把 {mat['code']} 在 {loc['code']} 的庫存調整為 {STOCK_QTY} ===")
adjust = call("POST", "/inventory/adjust", {
    "locationId": loc["id"],
    "materialId": mat["id"],
    "newQuantity": STOCK_QTY,
    "reason": "併發測試前置準備",
})
if not adjust.get("success"):
    print("  準備失敗：", adjust)
    sys.exit(1)
check("起始可用量", adjust["data"]["availableQuantity"], STOCK_QTY)

print(f"\n=== 建立 {ORDER_COUNT} 張各需求 {ORDER_QTY} 的出庫單 ===")
orders = []
for _ in range(ORDER_COUNT):
    order = call("POST", "/outbound-orders", {
        "warehouseId": wh["id"],
        "customerName": "併發測試",
        "details": [{"materialId": mat["id"], "requestedQuantity": ORDER_QTY}],
    })["data"]
    orders.append(order)
print(f"  已建立：{', '.join(o['orderNo'] for o in orders)}")

print(f"\n=== 同時送出 {ORDER_COUNT} 個分配請求 ===")
results = {}
barrier = threading.Barrier(ORDER_COUNT)


def allocate(order):
    barrier.wait()  # 讓所有執行緒盡可能在同一瞬間打進去
    results[order["orderNo"]] = call("POST", f"/outbound-orders/{order['id']}/allocate")


threads = [threading.Thread(target=allocate, args=(o,)) for o in orders]
for t in threads:
    t.start()
for t in threads:
    t.join()

allocated_total = 0
success_count = 0
for order_no, payload in sorted(results.items()):
    if payload.get("success"):
        qty = payload["data"]["lines"][0]["allocatedQuantity"]
        allocated_total += qty
        success_count += 1
        print(f"  {order_no}: 分配成功 {qty}")
    else:
        code = payload.get("errors", [{}])[0].get("code", "?")
        print(f"  {order_no}: 分配失敗（{code}）— {payload.get('message')}")

print(f"\n=== 結果檢查 ===")
inv = next(
    i for i in call("GET", f"/inventory?materialId={mat['id']}&onlyInStock=true")["data"]["items"]
    if i["locationCode"] == loc["code"]
)

print(f"  成功分配 {success_count} 張，分配總量 {allocated_total}")
print(f"  庫存：quantity={inv['quantity']} reserved={inv['reservedQuantity']} available={inv['availableQuantity']}")

check("分配總量未超過可用庫存", allocated_total <= STOCK_QTY, True)
check("預留量未超過實際庫存", inv["reservedQuantity"] <= inv["quantity"], True)
check("可用量未變成負數", inv["availableQuantity"] >= 0, True)
check("預留量等於分配總量", inv["reservedQuantity"], allocated_total)

# 收尾：釋放所有預留，讓資料庫回到乾淨狀態。
for order in orders:
    call("POST", f"/outbound-orders/{order['id']}/release")
    call("POST", f"/outbound-orders/{order['id']}/cancel")

print()
if FAILED:
    print(f"XXX {len(FAILED)} 項檢查失敗：{FAILED}")
    sys.exit(1)
print(">>> 併發分配未發生超賣，樂觀鎖與資料庫檢查條件正確生效。")
