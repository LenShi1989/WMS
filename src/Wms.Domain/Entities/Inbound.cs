using Wms.Domain.Common;
using Wms.Domain.Enums;

namespace Wms.Domain.Entities;

/// <summary>入庫單。</summary>
public class InboundOrder : AuditableEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public InboundSourceType SourceType { get; set; } = InboundSourceType.MANUAL;
    public string? ExternalOrderNo { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public Guid WarehouseId { get; set; }
    public InboundStatus Status { get; set; } = InboundStatus.DRAFT;
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Remark { get; set; }
    public Guid? CreatedBy { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<InboundDetail> Details { get; set; } = [];
}

/// <summary>入庫單明細。</summary>
public class InboundDetail : BaseEntity
{
    public Guid InboundOrderId { get; set; }
    public int LineNo { get; set; }
    public Guid MaterialId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PutawayQuantity { get; set; }
    public InboundDetailStatus Status { get; set; } = InboundDetailStatus.PENDING;
    public string? Remark { get; set; }

    public InboundOrder InboundOrder { get; set; } = null!;
    public Material Material { get; set; } = null!;
    public ICollection<PutawayTask> PutawayTasks { get; set; } = [];
}

/// <summary>上架任務：把收貨區的貨搬到儲存儲位。</summary>
public class PutawayTask : BaseEntity
{
    public string TaskNo { get; set; } = string.Empty;
    public Guid InboundDetailId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? TargetLocationId { get; set; }
    public WmsTaskStatus Status { get; set; } = WmsTaskStatus.PENDING;
    public int Priority { get; set; } = 5;
    public Guid? AssignedUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public InboundDetail InboundDetail { get; set; } = null!;
    public Material Material { get; set; } = null!;
    public Location? SourceLocation { get; set; }
    public Location? TargetLocation { get; set; }
}
