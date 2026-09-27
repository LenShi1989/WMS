using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Dtos;

public class InboundOrderDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public InboundSourceType SourceType { get; set; }
    public string? ExternalOrderNo { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public InboundStatus Status { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineCount { get; set; }
    public decimal TotalOrderedQuantity { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public List<InboundDetailDto> Details { get; set; } = [];
}

public class InboundDetailDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PutawayQuantity { get; set; }
    public InboundDetailStatus Status { get; set; }
    public string? Remark { get; set; }
}

public class InboundOrderCreateDto
{
    public InboundSourceType SourceType { get; set; } = InboundSourceType.MANUAL;
    [MaxLength(100)] public string? ExternalOrderNo { get; set; }
    [MaxLength(50)] public string? SupplierCode { get; set; }
    [MaxLength(200)] public string? SupplierName { get; set; }
    [Required] public Guid WarehouseId { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    [MaxLength(500)] public string? Remark { get; set; }
    [Required, MinLength(1)] public List<InboundLineDto> Details { get; set; } = [];
}

public class InboundLineDto
{
    [Required] public Guid MaterialId { get; set; }
    [Range(0.000001, double.MaxValue)] public decimal OrderedQuantity { get; set; }
    [MaxLength(500)] public string? Remark { get; set; }
}

public class InboundQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public InboundStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

/// <summary>收貨請求。</summary>
public class ReceiveRequest
{
    [Required, MinLength(1)] public List<ReceiveLineDto> Lines { get; set; } = [];

    /// <summary>收貨暫存儲位；未指定時自動取該倉庫的收貨區儲位。</summary>
    public Guid? ReceivingLocationId { get; set; }

    [MaxLength(500)] public string? Remark { get; set; }
}

public class ReceiveLineDto
{
    [Required] public Guid InboundDetailId { get; set; }
    [Range(0.000001, double.MaxValue)] public decimal ReceivedQuantity { get; set; }
}

public class PutawayTaskDto
{
    public Guid Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public Guid InboundDetailId { get; set; }
    public string InboundOrderNo { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? SourceLocationId { get; set; }
    public string? SourceLocationCode { get; set; }
    public Guid? TargetLocationId { get; set; }
    public string? TargetLocationCode { get; set; }
    public WmsTaskStatus Status { get; set; }
    public int Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PutawayQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public WmsTaskStatus? Status { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? MaterialId { get; set; }
}

public class AssignTaskRequest
{
    [Required] public Guid UserId { get; set; }
}

public class PutawayCompleteRequest
{
    /// <summary>實際上架儲位；未指定時沿用任務建議的目標儲位。</summary>
    public Guid? TargetLocationId { get; set; }

    /// <summary>實際上架數量；未指定時視為全數上架。</summary>
    public decimal? Quantity { get; set; }
}
