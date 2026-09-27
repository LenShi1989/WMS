using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Dtos;

// ---------- 倉庫 ----------
public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ZoneCount { get; set; }
    public int LocationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WarehouseSaveDto
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---------- 儲區 ----------
public class ZoneDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ZoneType ZoneType { get; set; }
    public bool IsActive { get; set; }
    public int LocationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ZoneSaveDto
{
    [Required] public Guid WarehouseId { get; set; }
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public ZoneType ZoneType { get; set; } = ZoneType.STORAGE;
    public bool IsActive { get; set; } = true;
}

public class ZoneQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public ZoneType? ZoneType { get; set; }
    public bool? IsActive { get; set; }
}

// ---------- 儲位 ----------
public class LocationDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public Guid? ZoneId { get; set; }
    public string? ZoneCode { get; set; }
    public ZoneType? ZoneType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public decimal Capacity { get; set; }
    public decimal WeightLimit { get; set; }
    public bool IsActive { get; set; }
    public decimal OnHandQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LocationSaveDto
{
    [Required] public Guid WarehouseId { get; set; }
    public Guid? ZoneId { get; set; }
    [Required, MaxLength(100)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public LocationType LocationType { get; set; } = LocationType.SHELF;
    [Range(0, double.MaxValue)] public decimal Capacity { get; set; }
    [Range(0, double.MaxValue)] public decimal WeightLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LocationQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? ZoneId { get; set; }
    public LocationType? LocationType { get; set; }
    public bool? IsActive { get; set; }
}
