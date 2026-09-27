using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Dtos;

public class TransferOrderDto
{
    public Guid Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public Guid FromLocationId { get; set; }
    public string FromLocationCode { get; set; } = string.Empty;
    public Guid ToLocationId { get; set; }
    public string ToLocationCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public TransferStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public string? ExecutedByName { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransferCreateDto
{
    [Required] public Guid MaterialId { get; set; }
    [Required] public Guid FromLocationId { get; set; }
    [Required] public Guid ToLocationId { get; set; }
    [Range(0.000001, double.MaxValue)] public decimal Quantity { get; set; }
    [MaxLength(200)] public string? Reason { get; set; }
    [MaxLength(500)] public string? Remark { get; set; }

    /// <summary>建立後是否立即執行移庫。</summary>
    public bool ExecuteImmediately { get; set; }
}

public class TransferQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? MaterialId { get; set; }
    public TransferStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
