using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Dtos;

public class InventoryDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public InventoryStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InventoryQuery : PagedQuery
{
    public Guid? MaterialId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public InventoryStatus? Status { get; set; }
    /// <summary>只顯示有庫存（數量 > 0）的紀錄。</summary>
    public bool? OnlyInStock { get; set; }
}

/// <summary>依物料彙總的庫存。</summary>
public class InventorySummaryDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public decimal SafetyStock { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public int LocationCount { get; set; }
    public bool BelowSafetyStock { get; set; }
}

public class InventoryTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public Guid? FromLocationId { get; set; }
    public string? FromLocationCode { get; set; }
    public Guid? ToLocationId { get; set; }
    public string? ToLocationCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal? BalanceAfter { get; set; }
    public ReferenceType? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public Guid? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionQuery : PagedQuery
{
    public Guid? MaterialId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public TransactionType? TransactionType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

/// <summary>庫存調整（人工異動）。</summary>
public class InventoryAdjustDto
{
    [Required] public Guid LocationId { get; set; }
    [Required] public Guid MaterialId { get; set; }
    /// <summary>調整後的數量（絕對值，非增減量）。</summary>
    [Required, Range(0, double.MaxValue)] public decimal NewQuantity { get; set; }
    [Required, MaxLength(200)] public string Reason { get; set; } = string.Empty;
}
