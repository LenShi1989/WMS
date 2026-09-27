using Wms.Domain.Common;
using Wms.Domain.Enums;

namespace Wms.Domain.Entities;

/// <summary>盤點單。</summary>
public class Stocktake : AuditableEntity
{
    public string StocktakeNo { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid? ZoneId { get; set; }
    public StocktakeStatus Status { get; set; } = StocktakeStatus.DRAFT;
    public string? Remark { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public WarehouseZone? Zone { get; set; }
    public ICollection<StocktakeDetail> Details { get; set; } = [];
}

/// <summary>盤點明細：建立盤點時 Snapshot 系統數量，盤完後比對差異。</summary>
public class StocktakeDetail : BaseEntity
{
    public Guid StocktakeId { get; set; }
    public Guid LocationId { get; set; }
    public Guid MaterialId { get; set; }

    /// <summary>建立盤點時的系統帳面數量（Snapshot）。</summary>
    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    /// <summary>差異 = 實盤 - 帳面。</summary>
    public decimal DifferenceQuantity { get; set; }
    public StocktakeDetailStatus Status { get; set; } = StocktakeDetailStatus.PENDING;
    public Guid? CountedBy { get; set; }
    public DateTime? CountedAt { get; set; }
    public string? Remark { get; set; }

    public Stocktake Stocktake { get; set; } = null!;
    public Location Location { get; set; } = null!;
    public Material Material { get; set; } = null!;
}
