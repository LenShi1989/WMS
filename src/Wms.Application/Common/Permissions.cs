namespace Wms.Application.Common;

/// <summary>
/// 全系統權限碼清單。命名一律為 MODULE_ACTION，
/// Controller 的 [Authorize(Policy = ...)] 與資料庫 permissions.code 共用同一組字串。
/// </summary>
public static class Permissions
{
    public const string MaterialView = "MATERIAL_VIEW";
    public const string MaterialCreate = "MATERIAL_CREATE";
    public const string MaterialUpdate = "MATERIAL_UPDATE";
    public const string MaterialDelete = "MATERIAL_DELETE";

    public const string CategoryView = "CATEGORY_VIEW";
    public const string CategoryManage = "CATEGORY_MANAGE";

    public const string UomView = "UOM_VIEW";
    public const string UomManage = "UOM_MANAGE";

    public const string BarcodeView = "BARCODE_VIEW";
    public const string BarcodeManage = "BARCODE_MANAGE";

    public const string WarehouseView = "WAREHOUSE_VIEW";
    public const string WarehouseManage = "WAREHOUSE_MANAGE";

    public const string ZoneView = "ZONE_VIEW";
    public const string ZoneManage = "ZONE_MANAGE";

    public const string LocationView = "LOCATION_VIEW";
    public const string LocationManage = "LOCATION_MANAGE";

    public const string InboundView = "INBOUND_VIEW";
    public const string InboundCreate = "INBOUND_CREATE";
    public const string InboundReceive = "INBOUND_RECEIVE";
    public const string InboundComplete = "INBOUND_COMPLETE";

    public const string PutawayView = "PUTAWAY_VIEW";
    public const string PutawayExecute = "PUTAWAY_EXECUTE";

    public const string OutboundView = "OUTBOUND_VIEW";
    public const string OutboundCreate = "OUTBOUND_CREATE";
    public const string OutboundAllocate = "OUTBOUND_ALLOCATE";
    public const string OutboundShip = "OUTBOUND_SHIP";

    public const string PickingView = "PICKING_VIEW";
    public const string PickingExecute = "PICKING_EXECUTE";

    public const string InventoryView = "INVENTORY_VIEW";
    public const string InventoryAdjust = "INVENTORY_ADJUST";

    public const string TransferView = "TRANSFER_VIEW";
    public const string TransferExecute = "TRANSFER_EXECUTE";

    public const string StocktakeView = "STOCKTAKE_VIEW";
    public const string StocktakeCreate = "STOCKTAKE_CREATE";
    public const string StocktakeCount = "STOCKTAKE_COUNT";
    public const string StocktakeApprove = "STOCKTAKE_APPROVE";

    public const string UserView = "USER_VIEW";
    public const string UserManage = "USER_MANAGE";

    public const string RoleView = "ROLE_VIEW";
    public const string RoleManage = "ROLE_MANAGE";

    public const string AuditView = "AUDIT_VIEW";
    public const string DashboardView = "DASHBOARD_VIEW";

    /// <summary>權限完整定義：Code / 名稱 / 模組 / 動作。Seed 與權限維護畫面共用。</summary>
    public static readonly IReadOnlyList<(string Code, string Name, string Module, string Action)> All =
    [
        (MaterialView,      "檢視物料",     "MATERIAL",   "VIEW"),
        (MaterialCreate,    "建立物料",     "MATERIAL",   "CREATE"),
        (MaterialUpdate,    "修改物料",     "MATERIAL",   "UPDATE"),
        (MaterialDelete,    "刪除物料",     "MATERIAL",   "DELETE"),

        (CategoryView,      "檢視物料分類", "CATEGORY",   "VIEW"),
        (CategoryManage,    "維護物料分類", "CATEGORY",   "MANAGE"),

        (UomView,           "檢視單位",     "UOM",        "VIEW"),
        (UomManage,         "維護單位",     "UOM",        "MANAGE"),

        (BarcodeView,       "檢視條碼",     "BARCODE",    "VIEW"),
        (BarcodeManage,     "維護條碼",     "BARCODE",    "MANAGE"),

        (WarehouseView,     "檢視倉庫",     "WAREHOUSE",  "VIEW"),
        (WarehouseManage,   "維護倉庫",     "WAREHOUSE",  "MANAGE"),

        (ZoneView,          "檢視儲區",     "ZONE",       "VIEW"),
        (ZoneManage,        "維護儲區",     "ZONE",       "MANAGE"),

        (LocationView,      "檢視儲位",     "LOCATION",   "VIEW"),
        (LocationManage,    "維護儲位",     "LOCATION",   "MANAGE"),

        (InboundView,       "檢視入庫單",   "INBOUND",    "VIEW"),
        (InboundCreate,     "建立入庫單",   "INBOUND",    "CREATE"),
        (InboundReceive,    "收貨",         "INBOUND",    "RECEIVE"),
        (InboundComplete,   "入庫結案",     "INBOUND",    "COMPLETE"),

        (PutawayView,       "檢視上架任務", "PUTAWAY",    "VIEW"),
        (PutawayExecute,    "執行上架",     "PUTAWAY",    "EXECUTE"),

        (OutboundView,      "檢視出庫單",   "OUTBOUND",   "VIEW"),
        (OutboundCreate,    "建立出庫單",   "OUTBOUND",   "CREATE"),
        (OutboundAllocate,  "庫存分配",     "OUTBOUND",   "ALLOCATE"),
        (OutboundShip,      "出貨",         "OUTBOUND",   "SHIP"),

        (PickingView,       "檢視揀貨任務", "PICKING",    "VIEW"),
        (PickingExecute,    "執行揀貨",     "PICKING",    "EXECUTE"),

        (InventoryView,     "檢視庫存",     "INVENTORY",  "VIEW"),
        (InventoryAdjust,   "庫存調整",     "INVENTORY",  "ADJUST"),

        (TransferView,      "檢視移庫",     "TRANSFER",   "VIEW"),
        (TransferExecute,   "執行移庫",     "TRANSFER",   "EXECUTE"),

        (StocktakeView,     "檢視盤點",     "STOCKTAKE",  "VIEW"),
        (StocktakeCreate,   "建立盤點",     "STOCKTAKE",  "CREATE"),
        (StocktakeCount,    "輸入盤點數",   "STOCKTAKE",  "COUNT"),
        (StocktakeApprove,  "核准盤點",     "STOCKTAKE",  "APPROVE"),

        (UserView,          "檢視使用者",   "USER",       "VIEW"),
        (UserManage,        "維護使用者",   "USER",       "MANAGE"),

        (RoleView,          "檢視角色",     "ROLE",       "VIEW"),
        (RoleManage,        "維護角色",     "ROLE",       "MANAGE"),

        (AuditView,         "檢視操作紀錄", "AUDIT",      "VIEW"),
        (DashboardView,     "檢視儀表板",   "DASHBOARD",  "VIEW")
    ];

    public static IEnumerable<string> AllCodes => All.Select(p => p.Code);
}

/// <summary>內建角色名稱。</summary>
public static class SystemRoles
{
    public const string Admin = "系統管理員";
    public const string WarehouseManager = "倉管主管";
    public const string Operator = "倉庫作業員";
    public const string Viewer = "唯讀使用者";
}
