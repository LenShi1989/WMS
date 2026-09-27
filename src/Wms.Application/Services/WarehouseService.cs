using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IWarehouseService
{
    Task<PagedResult<WarehouseDto>> QueryAsync(PagedQuery query, CancellationToken ct = default);
    Task<WarehouseDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<WarehouseDto> CreateAsync(WarehouseSaveDto dto, CancellationToken ct = default);
    Task<WarehouseDto> UpdateAsync(Guid id, WarehouseSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class WarehouseService(IWmsDbContext db, IAuditService audit) : IWarehouseService
{
    public Task<PagedResult<WarehouseDto>> QueryAsync(PagedQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Warehouses
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(keyword),
                w => w.Code.ToLower().Contains(keyword) || w.Name.ToLower().Contains(keyword))
            .OrderBy(w => w.Code);

        return q.ToPagedResultAsync(query, Projection, ct);
    }

    public async Task<WarehouseDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.Warehouses.AsNoTracking().Where(w => w.Id == id).Select(Projection).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的倉庫。");
    }

    public async Task<WarehouseDto> CreateAsync(WarehouseSaveDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.Warehouses.AnyAsync(w => w.Code == code, ct))
        {
            throw AppException.Conflict($"倉庫編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        var entity = new Warehouse
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        db.Warehouses.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "WAREHOUSE", "Warehouse", entity.Id, entity.Code, null, dto, ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, WarehouseSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的倉庫。");

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.Warehouses.AnyAsync(w => w.Code == code && w.Id != id, ct))
        {
            throw AppException.Conflict($"倉庫編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        entity.Code = code;
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "WAREHOUSE", "Warehouse", id, entity.Code, null, dto, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的倉庫。");

        if (await db.Inventories.AnyAsync(i => i.WarehouseId == id && i.Quantity != 0, ct))
        {
            throw AppException.Conflict("此倉庫尚有庫存，無法刪除；請改為停用。", "IN_USE");
        }

        if (await db.Locations.AnyAsync(l => l.WarehouseId == id, ct))
        {
            throw AppException.Conflict("請先刪除此倉庫底下的儲位。", "IN_USE");
        }

        db.Warehouses.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "WAREHOUSE", "Warehouse", id, entity.Code, entity.Code, null, ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Warehouse, WarehouseDto>> Projection =
        w => new WarehouseDto
        {
            Id = w.Id,
            Code = w.Code,
            Name = w.Name,
            Description = w.Description,
            IsActive = w.IsActive,
            ZoneCount = w.Zones.Count,
            LocationCount = w.Locations.Count,
            CreatedAt = w.CreatedAt
        };
}

public interface IZoneService
{
    Task<PagedResult<ZoneDto>> QueryAsync(ZoneQuery query, CancellationToken ct = default);
    Task<ZoneDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ZoneDto> CreateAsync(ZoneSaveDto dto, CancellationToken ct = default);
    Task<ZoneDto> UpdateAsync(Guid id, ZoneSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class ZoneService(IWmsDbContext db, IAuditService audit) : IZoneService
{
    public Task<PagedResult<ZoneDto>> QueryAsync(ZoneQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.WarehouseZones
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, z => z.WarehouseId == query.WarehouseId)
            .WhereIf(query.ZoneType.HasValue, z => z.ZoneType == query.ZoneType)
            .WhereIf(query.IsActive.HasValue, z => z.IsActive == query.IsActive)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                z => z.Code.ToLower().Contains(keyword) || z.Name.ToLower().Contains(keyword))
            .OrderBy(z => z.Warehouse.Code)
            .ThenBy(z => z.Code);

        return q.ToPagedResultAsync(query, Projection, ct);
    }

    public async Task<ZoneDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.WarehouseZones.AsNoTracking().Where(z => z.Id == id).Select(Projection).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的儲區。");
    }

    public async Task<ZoneDto> CreateAsync(ZoneSaveDto dto, CancellationToken ct = default)
    {
        if (!await db.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId, ct))
        {
            throw AppException.NotFound("找不到指定的倉庫。");
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.WarehouseZones.AnyAsync(z => z.WarehouseId == dto.WarehouseId && z.Code == code, ct))
        {
            throw AppException.Conflict($"此倉庫已有儲區編號 {code}。", "DUPLICATE_CODE");
        }

        var entity = new WarehouseZone
        {
            WarehouseId = dto.WarehouseId,
            Code = code,
            Name = dto.Name.Trim(),
            ZoneType = dto.ZoneType,
            IsActive = dto.IsActive
        };

        db.WarehouseZones.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "ZONE", "WarehouseZone", entity.Id, entity.Code, null, dto, ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task<ZoneDto> UpdateAsync(Guid id, ZoneSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.WarehouseZones.FirstOrDefaultAsync(z => z.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的儲區。");

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.WarehouseZones.AnyAsync(z => z.WarehouseId == entity.WarehouseId && z.Code == code && z.Id != id, ct))
        {
            throw AppException.Conflict($"此倉庫已有儲區編號 {code}。", "DUPLICATE_CODE");
        }

        entity.Code = code;
        entity.Name = dto.Name.Trim();
        entity.ZoneType = dto.ZoneType;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "ZONE", "WarehouseZone", id, entity.Code, null, dto, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.WarehouseZones.FirstOrDefaultAsync(z => z.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的儲區。");

        if (await db.Locations.AnyAsync(l => l.ZoneId == id, ct))
        {
            throw AppException.Conflict("此儲區底下仍有儲位，無法刪除。", "IN_USE");
        }

        db.WarehouseZones.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "ZONE", "WarehouseZone", id, entity.Code, entity.Code, null, ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<WarehouseZone, ZoneDto>> Projection =
        z => new ZoneDto
        {
            Id = z.Id,
            WarehouseId = z.WarehouseId,
            WarehouseCode = z.Warehouse.Code,
            Code = z.Code,
            Name = z.Name,
            ZoneType = z.ZoneType,
            IsActive = z.IsActive,
            LocationCount = z.Locations.Count,
            CreatedAt = z.CreatedAt
        };
}

public interface ILocationService
{
    Task<PagedResult<LocationDto>> QueryAsync(LocationQuery query, CancellationToken ct = default);
    Task<LocationDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<LocationDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<LocationDto> CreateAsync(LocationSaveDto dto, CancellationToken ct = default);
    Task<LocationDto> UpdateAsync(Guid id, LocationSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class LocationService(IWmsDbContext db, IAuditService audit) : ILocationService
{
    public Task<PagedResult<LocationDto>> QueryAsync(LocationQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Locations
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, l => l.WarehouseId == query.WarehouseId)
            .WhereIf(query.ZoneId.HasValue, l => l.ZoneId == query.ZoneId)
            .WhereIf(query.LocationType.HasValue, l => l.LocationType == query.LocationType)
            .WhereIf(query.IsActive.HasValue, l => l.IsActive == query.IsActive)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                l => l.Code.ToLower().Contains(keyword) || l.Name.ToLower().Contains(keyword))
            .OrderBy(l => l.Code);

        return q.ToPagedResultAsync(query, Projection(db), ct);
    }

    public async Task<LocationDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.Locations.AsNoTracking().Where(l => l.Id == id).Select(Projection(db)).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的儲位。");
    }

    public Task<LocationDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var target = code.Trim().ToUpperInvariant();
        return db.Locations.AsNoTracking()
            .Where(l => l.Code == target)
            .Select(Projection(db))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<LocationDto> CreateAsync(LocationSaveDto dto, CancellationToken ct = default)
    {
        await ValidateAsync(dto, ct);

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.Locations.AnyAsync(l => l.Code == code, ct))
        {
            throw AppException.Conflict($"儲位編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        var entity = new Location
        {
            WarehouseId = dto.WarehouseId,
            ZoneId = dto.ZoneId,
            Code = code,
            Name = string.IsNullOrWhiteSpace(dto.Name) ? code : dto.Name.Trim(),
            LocationType = dto.LocationType,
            Capacity = dto.Capacity,
            WeightLimit = dto.WeightLimit,
            IsActive = dto.IsActive
        };

        db.Locations.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "LOCATION", "Location", entity.Id, entity.Code, null, dto, ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task<LocationDto> UpdateAsync(Guid id, LocationSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的儲位。");

        await ValidateAsync(dto, ct);

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.Locations.AnyAsync(l => l.Code == code && l.Id != id, ct))
        {
            throw AppException.Conflict($"儲位編號 {code} 已存在。", "DUPLICATE_CODE");
        }

        entity.WarehouseId = dto.WarehouseId;
        entity.ZoneId = dto.ZoneId;
        entity.Code = code;
        entity.Name = string.IsNullOrWhiteSpace(dto.Name) ? code : dto.Name.Trim();
        entity.LocationType = dto.LocationType;
        entity.Capacity = dto.Capacity;
        entity.WeightLimit = dto.WeightLimit;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "LOCATION", "Location", id, entity.Code, null, dto, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的儲位。");

        if (await db.Inventories.AnyAsync(i => i.LocationId == id && i.Quantity != 0, ct))
        {
            throw AppException.Conflict("此儲位尚有庫存，無法刪除；請改為停用。", "IN_USE");
        }

        db.Locations.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "LOCATION", "Location", id, entity.Code, entity.Code, null, ct);
    }

    private async Task ValidateAsync(LocationSaveDto dto, CancellationToken ct)
    {
        if (!await db.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId, ct))
        {
            throw AppException.NotFound("找不到指定的倉庫。");
        }

        if (dto.ZoneId.HasValue)
        {
            var zone = await db.WarehouseZones.AsNoTracking().FirstOrDefaultAsync(z => z.Id == dto.ZoneId, ct)
                ?? throw AppException.NotFound("找不到指定的儲區。");

            if (zone.WarehouseId != dto.WarehouseId)
            {
                throw new AppException("儲區不屬於所選倉庫。", "INVALID_ZONE");
            }
        }
    }

    private static System.Linq.Expressions.Expression<Func<Location, LocationDto>> Projection(IWmsDbContext db) =>
        l => new LocationDto
        {
            Id = l.Id,
            WarehouseId = l.WarehouseId,
            WarehouseCode = l.Warehouse.Code,
            ZoneId = l.ZoneId,
            ZoneCode = l.Zone != null ? l.Zone.Code : null,
            ZoneType = l.Zone != null ? l.Zone.ZoneType : (ZoneType?)null,
            Code = l.Code,
            Name = l.Name,
            LocationType = l.LocationType,
            Capacity = l.Capacity,
            WeightLimit = l.WeightLimit,
            IsActive = l.IsActive,
            OnHandQuantity = db.Inventories.Where(i => i.LocationId == l.Id).Sum(i => (decimal?)i.Quantity) ?? 0,
            CreatedAt = l.CreatedAt
        };
}
