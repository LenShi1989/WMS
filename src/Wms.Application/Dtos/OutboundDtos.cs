using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Dtos;

public class OutboundOrderDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? ExternalOrderNo { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public OutboundStatus Status { get; set; }
    public int Priority { get; set; }
    public DateTime? RequestedShipDate { get; set; }
    public DateTime? ShippedAt { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineCount { get; set; }
    public decimal TotalRequestedQuantity { get; set; }
    public decimal TotalAllocatedQuantity { get; set; }
    public decimal TotalPickedQuantity { get; set; }
    public List<OutboundDetailDto> Details { get; set; } = [];
}

public class OutboundDetailDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public OutboundDetailStatus Status { get; set; }
    public string? Remark { get; set; }
}

public class OutboundOrderCreateDto
{
    [MaxLength(100)] public string? ExternalOrderNo { get; set; }
    [MaxLength(50)] public string? CustomerCode { get; set; }
    [MaxLength(200)] public string? CustomerName { get; set; }
    [Required] public Guid WarehouseId { get; set; }
    [Range(1, 9)] public int Priority { get; set; } = 5;
    public DateTime? RequestedShipDate { get; set; }
    [MaxLength(500)] public string? Remark { get; set; }
    [Required, MinLength(1)] public List<OutboundLineDto> Details { get; set; } = [];
}

public class OutboundLineDto
{
    [Required] public Guid MaterialId { get; set; }
    [Range(0.000001, double.MaxValue)] public decimal RequestedQuantity { get; set; }
    [MaxLength(500)] public string? Remark { get; set; }
}

public class OutboundQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public OutboundStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

/// <summary>庫存分配結果。</summary>
public class AllocationResultDto
{
    public Guid OutboundOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public OutboundStatus Status { get; set; }
    public bool FullyAllocated { get; set; }
    public int CreatedPickTaskCount { get; set; }
    public List<AllocationLineDto> Lines { get; set; } = [];
}

public class AllocationLineDto
{
    public Guid OutboundDetailId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
}

public class PickTaskDto
{
    public Guid Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public Guid OutboundDetailId { get; set; }
    public string OutboundOrderNo { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public Guid SourceLocationId { get; set; }
    public string SourceLocationCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public WmsTaskStatus Status { get; set; }
    public int Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PickQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public WmsTaskStatus? Status { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? OutboundOrderId { get; set; }
}

public class PickCompleteRequest
{
    /// <summary>實際揀出數量；未指定時視為全數揀出。</summary>
    public decimal? PickedQuantity { get; set; }
}

public class ShipRequest
{
    [MaxLength(500)] public string? Remark { get; set; }
}
