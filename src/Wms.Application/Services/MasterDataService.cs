using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;

namespace Wms.Application.Services;

public interface IMaterialService
{
    Task<PagedResult<MaterialDto>> QueryAsync(MaterialQuery query, CancellationToken ct = default);
    Task<MaterialDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<MaterialDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<MaterialDto> CreateAsync(MaterialSaveDto dto, CancellationToken ct = default);
    Task<MaterialDto> UpdateAsync(Guid id, MaterialSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class MaterialService(IWmsDbContext db, IAuditService audit) : IMaterialService
{
    public async Task<PagedResult<MaterialDto>> QueryAsync(MaterialQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Materials
            .AsNoTracking()
            .Include(m => m.Category)
            .WhereIf(query.CategoryId.HasValue, m => m.CategoryId == query.CategoryId)
            .WhereIf(query.IsActive.HasValue, m => m.IsActive == query.IsActive)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                m => m.Code.ToLower().Contains(keyword)
                     || m.Name.ToLower().Contains(keyword)
                     || (m.Specification != null && m.Specification.ToLower().Contains(keyword)))
            .OrderBy(m => m.Code);

        return await q.ToPagedResultAsync(query, m => new MaterialDto
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            Specification = m.Specification,
            CategoryId = m.CategoryId,
            CategoryName = m.Category != null ? m.Category.Name : null,
            BaseUom = m.BaseUom,
            IsActive = m.IsActive,
            SafetyStock = m.SafetyStock,
            MinStock = m.MinStock,
            MaxStock = m.MaxStock,
            OnHandQuantity = db.Inventories.Where(i => i.MaterialId == m.Id).Sum(i => (decimal?)i.Quantity) ?? 0,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }, ct);
    }

    public async Task<MaterialDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var material = await db.Materials
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Barcodes)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的物料。");

        var onHand = await db.Inventories
            .Where(i => i.MaterialId == id)
            .SumAsync(i => (decimal?)i.Quantity, ct) ?? 0;

        return ToDto(material, onHand);
    }

    public async Task<MaterialDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var code = barcode.Trim();

        var material = await db.Materials
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Barcodes)
            .FirstOrDefaultAsync(m => m.Code == code || m.Barcodes.Any(b => b.Barcode == code), ct);

        if (material is null)
        {
            return null;
        }

        var onHand = await db.Inventories
            .Where(i => i.MaterialId == material.Id)
            .SumAsync(i => (decimal?)i.Quantity, ct) ?? 0;

        return ToDto(material, onHand);
    }

    public async Task<MaterialDto> CreateAsync(MaterialSaveDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.Materials.AnyAsync(m => m.Code == code, ct))
        {
            throw AppException.Conflict($"物料編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        await EnsureCategoryAsync(dto.CategoryId, ct);

        var material = new Material
        {
            Code = code,
            Name = dto.Name.Trim(),
            Specification = dto.Specification,
            CategoryId = dto.CategoryId,
            BaseUom = dto.BaseUom.Trim().ToUpperInvariant(),
            IsActive = dto.IsActive,
            SafetyStock = dto.SafetyStock,
            MinStock = dto.MinStock,
            MaxStock = dto.MaxStock
        };

        db.Materials.Add(material);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "MATERIAL", "Material", material.Id, material.Code, null, dto, ct);

        return await GetAsync(material.Id, ct);
    }

    public async Task<MaterialDto> UpdateAsync(Guid id, MaterialSaveDto dto, CancellationToken ct = default)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的物料。");

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.Materials.AnyAsync(m => m.Code == code && m.Id != id, ct))
        {
            throw AppException.Conflict($"物料編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        await EnsureCategoryAsync(dto.CategoryId, ct);

        var old = new { material.Code, material.Name, material.IsActive, material.SafetyStock };

        material.Code = code;
        material.Name = dto.Name.Trim();
        material.Specification = dto.Specification;
        material.CategoryId = dto.CategoryId;
        material.BaseUom = dto.BaseUom.Trim().ToUpperInvariant();
        material.IsActive = dto.IsActive;
        material.SafetyStock = dto.SafetyStock;
        material.MinStock = dto.MinStock;
        material.MaxStock = dto.MaxStock;
        material.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "MATERIAL", "Material", material.Id, material.Code, old, dto, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的物料。");

        if (await db.Inventories.AnyAsync(i => i.MaterialId == id && i.Quantity != 0, ct))
        {
            throw AppException.Conflict("此物料尚有庫存，無法刪除；請改為停用。", "IN_USE");
        }

        if (await db.InventoryTransactions.AnyAsync(t => t.MaterialId == id, ct))
        {
            throw AppException.Conflict("此物料已有異動紀錄，無法刪除；請改為停用。", "IN_USE");
        }

        db.Materials.Remove(material);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "MATERIAL", "Material", id, material.Code, material.Code, null, ct);
    }

    private async Task EnsureCategoryAsync(Guid? categoryId, CancellationToken ct)
    {
        if (categoryId.HasValue && !await db.MaterialCategories.AnyAsync(c => c.Id == categoryId, ct))
        {
            throw AppException.NotFound("找不到指定的物料分類。");
        }
    }

    private static MaterialDto ToDto(Material m, decimal onHand) => new()
    {
        Id = m.Id,
        Code = m.Code,
        Name = m.Name,
        Specification = m.Specification,
        CategoryId = m.CategoryId,
        CategoryName = m.Category?.Name,
        BaseUom = m.BaseUom,
        IsActive = m.IsActive,
        SafetyStock = m.SafetyStock,
        MinStock = m.MinStock,
        MaxStock = m.MaxStock,
        OnHandQuantity = onHand,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        Barcodes = [.. m.Barcodes.Select(b => new MaterialBarcodeDto
        {
            Id = b.Id,
            MaterialId = b.MaterialId,
            MaterialCode = m.Code,
            MaterialName = m.Name,
            Barcode = b.Barcode,
            BarcodeType = b.BarcodeType,
            IsPrimary = b.IsPrimary,
            CreatedAt = b.CreatedAt
        })]
    };
}

public interface IMaterialCategoryService
{
    Task<PagedResult<MaterialCategoryDto>> QueryAsync(PagedQuery query, CancellationToken ct = default);
    Task<MaterialCategoryDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<MaterialCategoryDto> CreateAsync(MaterialCategorySaveDto dto, CancellationToken ct = default);
    Task<MaterialCategoryDto> UpdateAsync(Guid id, MaterialCategorySaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class MaterialCategoryService(IWmsDbContext db, IAuditService audit) : IMaterialCategoryService
{
    public Task<PagedResult<MaterialCategoryDto>> QueryAsync(PagedQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.MaterialCategories
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(keyword),
                c => c.Code.ToLower().Contains(keyword) || c.Name.ToLower().Contains(keyword))
            .OrderBy(c => c.Code);

        return q.ToPagedResultAsync(query, c => new MaterialCategoryDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            MaterialCount = c.Materials.Count,
            CreatedAt = c.CreatedAt
        }, ct);
    }

    public async Task<MaterialCategoryDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.MaterialCategories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new MaterialCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                MaterialCount = c.Materials.Count,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return dto ?? throw AppException.NotFound("找不到指定的物料分類。");
    }

    public async Task<MaterialCategoryDto> CreateAsync(MaterialCategorySaveDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.MaterialCategories.AnyAsync(c => c.Code == code, ct))
        {
            throw AppException.Conflict($"分類編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        var entity = new MaterialCategory
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        db.MaterialCategories.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "MATERIAL_CATEGORY", "MaterialCategory", entity.Id, entity.Code, null, dto, ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task<MaterialCategoryDto> UpdateAsync(Guid id, MaterialCategorySaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.MaterialCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的物料分類。");

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.MaterialCategories.AnyAsync(c => c.Code == code && c.Id != id, ct))
        {
            throw AppException.Conflict($"分類編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        entity.Code = code;
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "MATERIAL_CATEGORY", "MaterialCategory", id, entity.Code, null, dto, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.MaterialCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的物料分類。");

        if (await db.Materials.AnyAsync(m => m.CategoryId == id, ct))
        {
            throw AppException.Conflict("此分類下仍有物料，無法刪除。", "IN_USE");
        }

        db.MaterialCategories.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "MATERIAL_CATEGORY", "MaterialCategory", id, entity.Code, entity.Code, null, ct);
    }
}

public interface IUomService
{
    Task<PagedResult<UomDto>> QueryAsync(PagedQuery query, CancellationToken ct = default);
    Task<UomDto> CreateAsync(UomSaveDto dto, CancellationToken ct = default);
    Task<UomDto> UpdateAsync(Guid id, UomSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class UomService(IWmsDbContext db) : IUomService
{
    public Task<PagedResult<UomDto>> QueryAsync(PagedQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.UnitOfMeasures
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(keyword),
                u => u.Code.ToLower().Contains(keyword) || u.Name.ToLower().Contains(keyword))
            .OrderBy(u => u.Code);

        return q.ToPagedResultAsync(query, u => new UomDto
        {
            Id = u.Id,
            Code = u.Code,
            Name = u.Name,
            Description = u.Description,
            IsActive = u.IsActive
        }, ct);
    }

    public async Task<UomDto> CreateAsync(UomSaveDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.UnitOfMeasures.AnyAsync(u => u.Code == code, ct))
        {
            throw AppException.Conflict($"單位代碼 {code} 已存在。", "DUPLICATE_CODE");
        }

        var entity = new UnitOfMeasure
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        db.UnitOfMeasures.Add(entity);
        await db.SaveChangesAsync(ct);

        return new UomDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task<UomDto> UpdateAsync(Guid id, UomSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的單位。");

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.UnitOfMeasures.AnyAsync(u => u.Code == code && u.Id != id, ct))
        {
            throw AppException.Conflict($"單位代碼 {code} 已存在。", "DUPLICATE_CODE");
        }

        entity.Code = code;
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new UomDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的單位。");

        if (await db.Materials.AnyAsync(m => m.BaseUom == entity.Code, ct))
        {
            throw AppException.Conflict("此單位仍被物料使用，無法刪除。", "IN_USE");
        }

        db.UnitOfMeasures.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}

public interface IBarcodeService
{
    Task<PagedResult<MaterialBarcodeDto>> QueryAsync(BarcodeQuery query, CancellationToken ct = default);
    Task<MaterialBarcodeDto> CreateAsync(MaterialBarcodeSaveDto dto, CancellationToken ct = default);
    Task<MaterialBarcodeDto> UpdateAsync(Guid id, MaterialBarcodeSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class BarcodeService(IWmsDbContext db, IAuditService audit) : IBarcodeService
{
    public Task<PagedResult<MaterialBarcodeDto>> QueryAsync(BarcodeQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.MaterialBarcodes
            .AsNoTracking()
            .Include(b => b.Material)
            .WhereIf(query.MaterialId.HasValue, b => b.MaterialId == query.MaterialId)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                b => b.Barcode.ToLower().Contains(keyword)
                     || b.Material.Code.ToLower().Contains(keyword)
                     || b.Material.Name.ToLower().Contains(keyword))
            .OrderBy(b => b.Material.Code)
            .ThenByDescending(b => b.IsPrimary);

        return q.ToPagedResultAsync(query, b => new MaterialBarcodeDto
        {
            Id = b.Id,
            MaterialId = b.MaterialId,
            MaterialCode = b.Material.Code,
            MaterialName = b.Material.Name,
            Barcode = b.Barcode,
            BarcodeType = b.BarcodeType,
            IsPrimary = b.IsPrimary,
            CreatedAt = b.CreatedAt
        }, ct);
    }

    public async Task<MaterialBarcodeDto> CreateAsync(MaterialBarcodeSaveDto dto, CancellationToken ct = default)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == dto.MaterialId, ct)
            ?? throw AppException.NotFound("找不到指定的物料。");

        var barcode = dto.Barcode.Trim();
        if (await db.MaterialBarcodes.AnyAsync(b => b.Barcode == barcode, ct))
        {
            throw AppException.Conflict($"條碼 {barcode} 已被使用。", "DUPLICATE_BARCODE");
        }

        if (dto.IsPrimary)
        {
            await ClearPrimaryAsync(dto.MaterialId, null, ct);
        }

        var entity = new MaterialBarcode
        {
            MaterialId = dto.MaterialId,
            Barcode = barcode,
            BarcodeType = dto.BarcodeType.Trim().ToUpperInvariant(),
            IsPrimary = dto.IsPrimary
        };

        db.MaterialBarcodes.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "BARCODE", "MaterialBarcode", entity.Id, barcode, null, dto, ct);

        return new MaterialBarcodeDto
        {
            Id = entity.Id,
            MaterialId = entity.MaterialId,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            Barcode = entity.Barcode,
            BarcodeType = entity.BarcodeType,
            IsPrimary = entity.IsPrimary,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<MaterialBarcodeDto> UpdateAsync(Guid id, MaterialBarcodeSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.MaterialBarcodes.Include(b => b.Material).FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的條碼。");

        var barcode = dto.Barcode.Trim();
        if (await db.MaterialBarcodes.AnyAsync(b => b.Barcode == barcode && b.Id != id, ct))
        {
            throw AppException.Conflict($"條碼 {barcode} 已被使用。", "DUPLICATE_BARCODE");
        }

        if (dto.IsPrimary)
        {
            await ClearPrimaryAsync(entity.MaterialId, id, ct);
        }

        entity.Barcode = barcode;
        entity.BarcodeType = dto.BarcodeType.Trim().ToUpperInvariant();
        entity.IsPrimary = dto.IsPrimary;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "BARCODE", "MaterialBarcode", id, barcode, null, dto, ct);

        return new MaterialBarcodeDto
        {
            Id = entity.Id,
            MaterialId = entity.MaterialId,
            MaterialCode = entity.Material.Code,
            MaterialName = entity.Material.Name,
            Barcode = entity.Barcode,
            BarcodeType = entity.BarcodeType,
            IsPrimary = entity.IsPrimary,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.MaterialBarcodes.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的條碼。");

        db.MaterialBarcodes.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "BARCODE", "MaterialBarcode", id, entity.Barcode, entity.Barcode, null, ct);
    }

    private async Task ClearPrimaryAsync(Guid materialId, Guid? exceptId, CancellationToken ct)
    {
        var others = await db.MaterialBarcodes
            .Where(b => b.MaterialId == materialId && b.IsPrimary && (exceptId == null || b.Id != exceptId))
            .ToListAsync(ct);

        foreach (var other in others)
        {
            other.IsPrimary = false;
        }
    }
}
