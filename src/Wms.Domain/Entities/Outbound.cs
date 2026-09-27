using Wms.Domain.Common;
using Wms.Domain.Enums;

namespace Wms.Domain.Entities;

/// <summary>出庫單。</summary>
public class OutboundOrder : AuditableEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public string? ExternalOrderNo { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public Guid WarehouseId { get; set; }
    public OutboundStatus Status { get; set; } = OutboundStatus.DRAFT;
    public int Priority { get; set; } = 5;
    public DateTime? RequestedShipDate { get; set; }
    public DateTime? ShippedAt { get; set; }
    public string? Remark { get; set; }
    public Guid? CreatedBy { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<OutboundDetail> Details { get; set; } = [];
}

/// <summary>出庫單明細。</summary>
public class OutboundDetail : BaseEntity
{
    public Guid OutboundOrderId { get; set; }
    public int LineNo { get; set; }
    public Guid MaterialId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public OutboundDetailStatus Status { get; set; } = OutboundDetailStatus.PENDING;
    public string? Remark { get; set; }

    public OutboundOrder OutboundOrder { get; set; } = null!;
    public Material Material { get; set; } = null!;
    public ICollection<PickTask> PickTasks { get; set; } = [];
}

/// <summary>揀貨任務：由庫存分配結果產生，一個儲位一張。</summary>
public class PickTask : BaseEntity
{
    public string TaskNo { get; set; } = string.Empty;
    public Guid OutboundDetailId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid SourceLocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public WmsTaskStatus Status { get; set; } = WmsTaskStatus.PENDING;
    public int Priority { get; set; } = 5;
    public Guid? AssignedUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public OutboundDetail OutboundDetail { get; set; } = null!;
    public Material Material { get; set; } = null!;
    public Location SourceLocation { get; set; } = null!;
}
