using Wms.Domain.Common;
using Wms.Domain.Enums;

namespace Wms.Domain.Entities;

/// <summary>
/// 即時庫存。系統核心 Table，以（倉庫 + 儲位 + 物料 + 狀態）為唯一鍵。
/// 數量只能由 InventoryService 透過 InventoryTransaction 變更。
/// </summary>
public class Inventory : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Guid LocationId { get; set; }
    public Guid MaterialId { get; set; }

    /// <summary>實際在儲位上的數量。</summary>
    public decimal Quantity { get; set; }

    /// <summary>已被出庫單預留、尚未揀出的數量。</summary>
    public decimal ReservedQuantity { get; set; }

    /// <summary>可用數量 = Quantity - ReservedQuantity，由資料庫 generated column 維護。</summary>
    public decimal AvailableQuantity { get; private set; }

    public InventoryStatus Status { get; set; } = InventoryStatus.NORMAL;

    /// <summary>PostgreSQL xmin 樂觀鎖，用於防止並行超賣。</summary>
    public uint Version { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Location Location { get; set; } = null!;
    public Material Material { get; set; } = null!;
}

/// <summary>
/// 庫存異動流水（Ledger）。任何庫存變化都必須留下一筆，不可由前端直接建立。
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public string TransactionNo { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public Guid MaterialId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }

    /// <summary>異動數量，一律為正值，方向由 TransactionType 與 From/To 決定。</summary>
    public decimal Quantity { get; set; }

    /// <summary>異動後該儲位的結存數量，方便追溯。</summary>
    public decimal? BalanceAfter { get; set; }

    public ReferenceType? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public Guid? OperatorId { get; set; }
    public string? Remark { get; set; }

    public Material Material { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Location? FromLocation { get; set; }
    public Location? ToLocation { get; set; }
}
