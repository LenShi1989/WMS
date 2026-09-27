using Wms.Domain.Common;

namespace Wms.Domain.Entities;

/// <summary>物料分類。</summary>
public class MaterialCategory : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Material> Materials { get; set; } = [];
}

/// <summary>計量單位。</summary>
public class UnitOfMeasure : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>物料主檔。</summary>
public class Material : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public Guid? CategoryId { get; set; }
    public string BaseUom { get; set; } = "PCS";
    public bool IsActive { get; set; } = true;
    public decimal SafetyStock { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }

    public MaterialCategory? Category { get; set; }
    public ICollection<MaterialBarcode> Barcodes { get; set; } = [];
}

/// <summary>物料條碼，一個物料可以有多組條碼。</summary>
public class MaterialBarcode : BaseEntity
{
    public Guid MaterialId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = "CODE128";
    public bool IsPrimary { get; set; }

    public Material Material { get; set; } = null!;
}
