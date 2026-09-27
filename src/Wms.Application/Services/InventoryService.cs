using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IInventoryService
{
    Task<PagedResult<InventoryDto>> QueryAsync(InventoryQuery query, CancellationToken ct = default);
    Task<InventoryDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<InventorySummaryDto>> SummaryAsync(InventoryQuery query, CancellationToken ct = default);
    Task<PagedResult<InventoryTransactionDto>> QueryTransactionsAsync(TransactionQuery query, CancellationToken ct = default);
    Task<InventoryTransactionDto> GetTransactionAsync(Guid id, CancellationToken ct = default);
    Task<InventoryDto> AdjustAsync(InventoryAdjustDto dto, CancellationToken ct = default);
}

public class InventoryService(
    IWmsDbContext db,
    IInventoryEngine engine,
    IAuditService audit) : IInventoryService
{
    public Task<PagedResult<InventoryDto>> QueryAsync(InventoryQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Inventories
            .AsNoTracking()
            .WhereIf(query.MaterialId.HasValue, i => i.MaterialId == query.MaterialId)
            .WhereIf(query.WarehouseId.HasValue, i => i.WarehouseId == query.WarehouseId)
            .WhereIf(query.LocationId.HasValue, i => i.LocationId == query.LocationId)
            .WhereIf(query.Status.HasValue, i => i.Status == query.Status)
            .WhereIf(query.OnlyInStock == true, i => i.Quantity > 0)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                i => i.Material.Code.ToLower().Contains(keyword)
                     || i.Material.Name.ToLower().Contains(keyword)
                     || i.Location.Code.ToLower().Contains(keyword))
            .OrderBy(i => i.Material.Code)
            .ThenBy(i => i.Location.Code);

        return q.ToPagedResultAsync(query, Projection, ct);
    }

    public async Task<InventoryDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.Inventories.AsNoTracking().Where(i => i.Id == id).Select(Projection).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的庫存紀錄。");
    }

    public async Task<PagedResult<InventorySummaryDto>> SummaryAsync(InventoryQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var grouped = db.Inventories
            .AsNoTracking()
            .WhereIf(query.MaterialId.HasValue, i => i.MaterialId == query.MaterialId)
            .WhereIf(query.WarehouseId.HasValue, i => i.WarehouseId == query.WarehouseId)
            .WhereIf(query.Status.HasValue, i => i.Status == query.Status)
            .WhereIf(query.OnlyInStock == true, i => i.Quantity > 0)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                i => i.Material.Code.ToLower().Contains(keyword)
                     || i.Material.Name.ToLower().Contains(keyword))
            .GroupBy(i => new { i.MaterialId, i.Material.Code, i.Material.Name, i.Material.BaseUom, i.Material.SafetyStock })
            .Select(g => new InventorySummaryDto
            {
                MaterialId = g.Key.MaterialId,
                MaterialCode = g.Key.Code,
                MaterialName = g.Key.Name,
                BaseUom = g.Key.BaseUom,
                SafetyStock = g.Key.SafetyStock,
                Quantity = g.Sum(i => i.Quantity),
                ReservedQuantity = g.Sum(i => i.ReservedQuantity),
                AvailableQuantity = g.Sum(i => i.Quantity - i.ReservedQuantity),
                LocationCount = g.Count(),
                BelowSafetyStock = g.Sum(i => i.Quantity) < g.Key.SafetyStock
            })
            .OrderBy(s => s.MaterialCode);

        var total = await grouped.CountAsync(ct);
        var items = await grouped.Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        return new PagedResult<InventorySummaryDto>(items, query.Page, query.PageSize, total);
    }

    public Task<PagedResult<InventoryTransactionDto>> QueryTransactionsAsync(TransactionQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.InventoryTransactions
            .AsNoTracking()
            .WhereIf(query.MaterialId.HasValue, t => t.MaterialId == query.MaterialId)
            .WhereIf(query.WarehouseId.HasValue, t => t.WarehouseId == query.WarehouseId)
            .WhereIf(query.LocationId.HasValue,
                t => t.FromLocationId == query.LocationId || t.ToLocationId == query.LocationId)
            .WhereIf(query.TransactionType.HasValue, t => t.TransactionType == query.TransactionType)
            .WhereIf(query.DateFrom.HasValue, t => t.CreatedAt >= query.DateFrom!.Value)
            .WhereIf(query.DateTo.HasValue, t => t.CreatedAt < query.DateTo!.Value.AddDays(1))
            .WhereIf(!string.IsNullOrEmpty(keyword),
                t => t.TransactionNo.ToLower().Contains(keyword)
                     || (t.ReferenceNo != null && t.ReferenceNo.ToLower().Contains(keyword))
                     || t.Material.Code.ToLower().Contains(keyword))
            .OrderByDescending(t => t.CreatedAt);

        return q.ToPagedResultAsync(query, TransactionProjection(db), ct);
    }

    public async Task<InventoryTransactionDto> GetTransactionAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(TransactionProjection(db))
            .FirstOrDefaultAsync(ct);

        return dto ?? throw AppException.NotFound("找不到指定的異動紀錄。");
    }

    /// <summary>人工調整庫存：以「調整後數量」為準，差額自動補成 ADJUSTMENT 異動。</summary>
    public async Task<InventoryDto> AdjustAsync(InventoryAdjustDto dto, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var location = await db.Locations.FirstOrDefaultAsync(l => l.Id == dto.LocationId, ct)
            ?? throw AppException.NotFound("找不到指定的儲位。");

        if (!await db.Materials.AnyAsync(m => m.Id == dto.MaterialId, ct))
        {
            throw AppException.NotFound("找不到指定的物料。");
        }

        var inventory = await engine.FindAsync(dto.LocationId, dto.MaterialId, ct: ct);
        var current = inventory?.Quantity ?? 0;
        var difference = dto.NewQuantity - current;

        if (difference == 0)
        {
            throw new AppException("調整後數量與現有庫存相同，無需調整。", "NO_CHANGE");
        }

        if (dto.NewQuantity < (inventory?.ReservedQuantity ?? 0))
        {
            throw AppException.Conflict(
                $"調整後數量不可小於已預留數量 {inventory!.ReservedQuantity:0.######}。", "RESERVED_CONFLICT");
        }

        var context = new InventoryMoveContext
        {
            WarehouseId = location.WarehouseId,
            LocationId = dto.LocationId,
            MaterialId = dto.MaterialId,
            Quantity = Math.Abs(difference),
            TransactionType = TransactionType.ADJUSTMENT,
            ReferenceType = ReferenceType.MANUAL,
            Remark = dto.Reason
        };

        if (difference > 0)
        {
            await engine.MoveInAsync(context, ct);
        }
        else
        {
            await engine.MoveOutAsync(context, consumeReservation: false, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("ADJUST", "INVENTORY", "Inventory", inventory?.Id, location.Code,
            new { quantity = current }, new { quantity = dto.NewQuantity, dto.Reason }, ct);

        var result = await engine.FindAsync(dto.LocationId, dto.MaterialId, ct: ct);
        return await GetAsync(result!.Id, ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Inventory, InventoryDto>> Projection =
        i => new InventoryDto
        {
            Id = i.Id,
            WarehouseId = i.WarehouseId,
            WarehouseCode = i.Warehouse.Code,
            LocationId = i.LocationId,
            LocationCode = i.Location.Code,
            MaterialId = i.MaterialId,
            MaterialCode = i.Material.Code,
            MaterialName = i.Material.Name,
            BaseUom = i.Material.BaseUom,
            Quantity = i.Quantity,
            ReservedQuantity = i.ReservedQuantity,
            AvailableQuantity = i.AvailableQuantity,
            Status = i.Status,
            UpdatedAt = i.UpdatedAt
        };

    private static System.Linq.Expressions.Expression<Func<InventoryTransaction, InventoryTransactionDto>> TransactionProjection(IWmsDbContext db) =>
        t => new InventoryTransactionDto
        {
            Id = t.Id,
            TransactionNo = t.TransactionNo,
            TransactionType = t.TransactionType,
            MaterialId = t.MaterialId,
            MaterialCode = t.Material.Code,
            MaterialName = t.Material.Name,
            WarehouseId = t.WarehouseId,
            WarehouseCode = t.Warehouse.Code,
            FromLocationId = t.FromLocationId,
            FromLocationCode = t.FromLocation != null ? t.FromLocation.Code : null,
            ToLocationId = t.ToLocationId,
            ToLocationCode = t.ToLocation != null ? t.ToLocation.Code : null,
            Quantity = t.Quantity,
            BalanceAfter = t.BalanceAfter,
            ReferenceType = t.ReferenceType,
            ReferenceId = t.ReferenceId,
            ReferenceNo = t.ReferenceNo,
            OperatorId = t.OperatorId,
            OperatorName = db.Users.Where(u => u.Id == t.OperatorId).Select(u => u.DisplayName).FirstOrDefault(),
            Remark = t.Remark,
            CreatedAt = t.CreatedAt
        };
}
