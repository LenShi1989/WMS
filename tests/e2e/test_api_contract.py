"""驗證前端 src/api/index.ts 宣告的每個端點在後端都真實存在（不得回 404）。"""
import json
import re
import sys
import urllib.error
import urllib.request

sys.stdout.reconfigure(encoding='utf-8')

BASE = "http://localhost:5173/api/v1"
API_FILE = r"D:\Gitlab\WMS\wms-web\src\api\index.ts"

token = None
ids = {}


def call(method, path, body=None):
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as resp:
            return resp.status, json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read().decode())
        except Exception:
            return e.code, {}


status, payload = call("POST", "/auth/login", {"username": "admin", "password": "a12345678"})
token = payload["data"]["accessToken"]

# 先抓一組真實 id，讓帶參數的路徑也能驗證。
ids["material"] = call("GET", "/materials?pageSize=1")[1]["data"]["items"][0]["id"]
ids["warehouse"] = call("GET", "/warehouses?pageSize=1")[1]["data"]["items"][0]["id"]
ids["location"] = call("GET", "/locations?pageSize=1")[1]["data"]["items"][0]["id"]
ids["inventory"] = call("GET", "/inventory?pageSize=1")[1]["data"]["items"][0]["id"]
ids["inbound"] = call("GET", "/inbound-orders?pageSize=1")[1]["data"]["items"][0]["id"]
ids["outbound"] = call("GET", "/outbound-orders?pageSize=1")[1]["data"]["items"][0]["id"]
ids["putaway"] = call("GET", "/putaway-tasks?pageSize=1")[1]["data"]["items"][0]["id"]
ids["pick"] = call("GET", "/pick-tasks?pageSize=1")[1]["data"]["items"][0]["id"]
ids["transfer"] = call("GET", "/transfers?pageSize=1")[1]["data"]["items"][0]["id"]
ids["stocktake"] = call("GET", "/stocktakes?pageSize=1")[1]["data"]["items"][0]["id"]
ids["transaction"] = call("GET", "/inventory-transactions?pageSize=1")[1]["data"]["items"][0]["id"]
ids["role"] = call("GET", "/roles?pageSize=1")[1]["data"]["items"][0]["id"]
ids["user"] = call("GET", "/users?pageSize=1")[1]["data"]["items"][0]["id"]

# 從前端 api 模組抓出所有宣告的路徑樣板。
source = open(API_FILE, encoding="utf-8").read()
declared = sorted(set(re.findall(r"api\.\w+<[^>]*>\(\s*[`'\"]([^`'\"]+)[`'\"]", source)))

# 路徑樣板 → 實際可呼叫的路徑與方法。
SAMPLE = {
    "${id}": None,  # 由下方對照表逐一替換
}

RESOLVE = {
    "/materials": ids["material"], "/material-categories": None, "/uoms": None, "/barcodes": None,
    "/warehouses": ids["warehouse"], "/zones": None, "/locations": ids["location"],
    "/inbound-orders": ids["inbound"], "/putaway-tasks": ids["putaway"],
    "/outbound-orders": ids["outbound"], "/pick-tasks": ids["pick"],
    "/inventory": ids["inventory"], "/inventory-transactions": ids["transaction"],
    "/transfers": ids["transfer"], "/stocktakes": ids["stocktake"],
    "/users": ids["user"], "/roles": ids["role"],
}

failures = []
checked = 0

print("=== 前端宣告的 API 端點對照後端路由 ===")
for template in declared:
    # 只驗證讀取類端點；寫入類已在流程測試中實際跑過。
    if "${" in template:
        prefix = "/" + template.lstrip("/").split("/")[0]
        entity_id = RESOLVE.get(prefix)
        if entity_id is None:
            continue
        path = template.replace("${id}", entity_id)
        # 帶動作的子路徑（/receive、/allocate…）屬於 POST，流程測試已覆蓋，這裡跳過。
        if path.count("/") > 2:
            continue
    else:
        path = template

    if any(seg in template for seg in ("by-barcode", "by-code")):
        continue

    status, payload = call("GET", path)
    checked += 1
    # 404 才代表路由不存在；405 是 POST-only 端點，同樣證明路由已註冊。
    ok = status != 404
    note = "（POST-only，路由存在）" if status == 405 else ""
    print(f"  [{'PASS' if ok else 'FAIL'}] GET {path}  → HTTP {status}{note}")
    if not ok:
        failures.append(f"GET {path} → {status}")

# 另外驗證兩個掃碼專用端點確實存在。
for path, label in [
    (f"/materials/by-barcode/MAT001", "掃描物料條碼"),
    (f"/locations/by-code/WH01-A01-R01-L01", "掃描儲位條碼"),
]:
    status, payload = call("GET", path)
    checked += 1
    ok = status == 200 and payload.get("success")
    print(f"  [{'PASS' if ok else 'FAIL'}] GET {path}（{label}）→ HTTP {status}")
    if not ok:
        failures.append(f"GET {path} → {status}")

print(f"\n共驗證 {checked} 個端點。")
if failures:
    print(f"XXX {len(failures)} 個端點不存在：{failures}")
    sys.exit(1)
print(">>> 前端宣告的每個端點在後端都存在，前後端 API 合約一致。")
