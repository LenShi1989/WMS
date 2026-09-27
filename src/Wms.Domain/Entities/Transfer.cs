using Wms.Domain.Common;
using Wms.Domain.Enums;

namespace Wms.Domain.Entities;

/// <summary>移庫單：把庫存從來源儲位搬到目的儲位。</summary>
public class TransferOrder : AuditableEntity
{
    public string TransferNo { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public decimal Quantity { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.DRAFT;
    public string? Reason { get; set; }
    public string? Remark { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? ExecutedBy { get; set; }
    public DateTime? ExecutedAt { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Material Material { get; set; } = null!;
    public Location FromLocation { get; set; } = null!;
    public Location ToLocation { get; set; } = null!;
}
