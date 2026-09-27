using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;

namespace Wms.Application.Dtos;

// ---------- 物料分類 ----------
public class MaterialCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int MaterialCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MaterialCategorySaveDto
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---------- 單位 ----------
public class UomDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class UomSaveDto
{
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---------- 物料 ----------
public class MaterialDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string BaseUom { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }
    public decimal OnHandQuantity { get; set; }
    public List<MaterialBarcodeDto> Barcodes { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MaterialSaveDto
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Specification { get; set; }
    public Guid? CategoryId { get; set; }
    [Required, MaxLength(20)] public string BaseUom { get; set; } = "PCS";
    public bool IsActive { get; set; } = true;
    [Range(0, double.MaxValue)] public decimal SafetyStock { get; set; }
    [Range(0, double.MaxValue)] public decimal MinStock { get; set; }
    [Range(0, double.MaxValue)] public decimal MaxStock { get; set; }
}

public class MaterialQuery : PagedQuery
{
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

// ---------- 條碼 ----------
public class MaterialBarcodeDto
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MaterialBarcodeSaveDto
{
    [Required] public Guid MaterialId { get; set; }
    [Required, MaxLength(100)] public string Barcode { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string BarcodeType { get; set; } = "CODE128";
    public bool IsPrimary { get; set; }
}

public class BarcodeQuery : PagedQuery
{
    public Guid? MaterialId { get; set; }
}
