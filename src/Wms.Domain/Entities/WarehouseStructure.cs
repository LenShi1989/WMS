using Wms.Domain.Common;
using Wms.Domain.Enums;

namespace Wms.Domain.Entities;

/// <summary>倉庫。</summary>
public class Warehouse : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WarehouseZone> Zones { get; set; } = [];
    public ICollection<Location> Locations { get; set; } = [];
}

/// <summary>儲區，例如收貨區、儲存區、揀貨區。</summary>
public class WarehouseZone : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ZoneType ZoneType { get; set; } = ZoneType.STORAGE;
    public bool IsActive { get; set; } = true;

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<Location> Locations { get; set; } = [];
}

/// <summary>儲位，例如 WH01-A01-R03-L02-B05。</summary>
public class Location : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Guid? ZoneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LocationType LocationType { get; set; } = LocationType.SHELF;
    public decimal Capacity { get; set; }
    public decimal WeightLimit { get; set; }
    public bool IsActive { get; set; } = true;

    public Warehouse Warehouse { get; set; } = null!;
    public WarehouseZone? Zone { get; set; }
}
