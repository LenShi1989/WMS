using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Dtos;

public class StocktakeDto
{
    public Guid Id { get; set; }
    public string StocktakeNo { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? ZoneId { get; set; }
    public string? ZoneCode { get; set; }
    public StocktakeStatus Status { get; set; }
    public string? Remark { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineCount { get; set; }
    public int CountedCount { get; set; }
    public int DifferenceCount { get; set; }
    public List<StocktakeDetailDto> Details { get; set; } = [];
}

public class StocktakeDetailDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BaseUom { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public StocktakeDetailStatus Status { get; set; }
    public string? CountedByName { get; set; }
    public DateTime? CountedAt { get; set; }
    public string? Remark { get; set; }
}

public class StocktakeCreateDto
{
    [Required] public Guid WarehouseId { get; set; }

    /// <summary>限定盤點某個儲區；未指定時盤整座倉庫。</summary>
    public Guid? ZoneId { get; set; }

    [MaxLength(500)] public string? Remark { get; set; }
}

public class StocktakeQuery : PagedQuery
{
    public Guid? WarehouseId { get; set; }
    public StocktakeStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class StocktakeCountRequest
{
    [Required, MinLength(1)] public List<StocktakeCountLineDto> Lines { get; set; } = [];
}

public class StocktakeCountLineDto
{
    [Required] public Guid DetailId { get; set; }
    [Range(0, double.MaxValue)] public decimal CountedQuantity { get; set; }
    [MaxLength(500)] public string? Remark { get; set; }
}

/// <summary>指定明細重新盤點。</summary>
public class StocktakeRecountRequest
{
    [Required, MinLength(1)] public List<Guid> DetailIds { get; set; } = [];
}
