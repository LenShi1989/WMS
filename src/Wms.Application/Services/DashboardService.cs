using Microsoft.EntityFrameworkCore;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<List<DashboardTrendDto>> GetTrendAsync(int days, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetWarehouseStockAsync(CancellationToken ct = default);
    Task<List<InventorySummaryDto>> GetLowStockAsync(int top, CancellationToken ct = default);
}

public class DashboardService(IWmsDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayTransactions = db.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow);

        var inboundToday = await todayTransactions
            .Where(t => t.TransactionType == TransactionType.RECEIVE)
            .SumAsync(t => (decimal?)t.Quantity, ct) ?? 0;

        var outboundToday = await todayTransactions
            .Where(t => t.TransactionType == TransactionType.SHIP)
            .SumAsync(t => (decimal?)t.Quantity, ct) ?? 0;

        var inventoryTotal = await db.Inventories.AsNoTracking().SumAsync(i => (decimal?)i.Quantity, ct) ?? 0;

        var belowSafety = await db.Materials
            .AsNoTracking()
            .Where(m => m.IsActive && m.SafetyStock > 0)
            .CountAsync(m => (db.Inventories.Where(i => i.MaterialId == m.Id).Sum(i => (decimal?)i.Quantity) ?? 0) < m.SafetyStock, ct);

        return new DashboardSummaryDto
        {
            InboundToday = inboundToday,
            OutboundToday = outboundToday,
            InventoryTotal = inventoryTotal,
            PendingPutaway = await db.PutawayTasks.CountAsync(
                t => t.Status == WmsTaskStatus.PENDING || t.Status == WmsTaskStatus.ASSIGNED || t.Status == WmsTaskStatus.PROCESSING, ct),
            PendingPicking = await db.PickTasks.CountAsync(
                t => t.Status == WmsTaskStatus.PENDING || t.Status == WmsTaskStatus.ASSIGNED || t.Status == WmsTaskStatus.PROCESSING, ct),
            PendingStocktake = await db.Stocktakes.CountAsync(
                s => s.Status == StocktakeStatus.DRAFT || s.Status == StocktakeStatus.COUNTING
                     || s.Status == StocktakeStatus.RECOUNT || s.Status == StocktakeStatus.PENDING_APPROVAL, ct),
            MaterialCount = await db.Materials.CountAsync(m => m.IsActive, ct),
            WarehouseCount = await db.Warehouses.CountAsync(w => w.IsActive, ct),
            LocationCount = await db.Locations.CountAsync(l => l.IsActive, ct),
            BelowSafetyStockCount = belowSafety,
            OpenInboundOrders = await db.InboundOrders.CountAsync(
                o => o.Status != InboundStatus.COMPLETED && o.Status != InboundStatus.CANCELLED, ct),
            OpenOutboundOrders = await db.OutboundOrders.CountAsync(
                o => o.Status != OutboundStatus.SHIPPED && o.Status != OutboundStatus.CANCELLED, ct)
        };
    }

    public async Task<List<DashboardTrendDto>> GetTrendAsync(int days, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);
        var from = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var rows = await db.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= from
                        && (t.TransactionType == TransactionType.RECEIVE || t.TransactionType == TransactionType.SHIP))
            .GroupBy(t => new { Date = t.CreatedAt.Date, t.TransactionType })
            .Select(g => new { g.Key.Date, g.Key.TransactionType, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        return [.. Enumerable.Range(0, days)
            .Select(offset =>
            {
                var date = from.AddDays(offset);
                return new DashboardTrendDto
                {
                    Date = date.ToString("MM/dd"),
                    Inbound = rows.FirstOrDefault(r => r.Date == date && r.TransactionType == TransactionType.RECEIVE)?.Quantity ?? 0,
                    Outbound = rows.FirstOrDefault(r => r.Date == date && r.TransactionType == TransactionType.SHIP)?.Quantity ?? 0
                };
            })];
    }

    public Task<List<WarehouseStockDto>> GetWarehouseStockAsync(CancellationToken ct = default)
    {
        return db.Inventories
            .AsNoTracking()
            .GroupBy(i => new { i.WarehouseId, i.Warehouse.Code, i.Warehouse.Name })
            .Select(g => new WarehouseStockDto
            {
                WarehouseCode = g.Key.Code,
                WarehouseName = g.Key.Name,
                Quantity = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(w => w.Quantity)
            .ToListAsync(ct);
    }

    public Task<List<InventorySummaryDto>> GetLowStockAsync(int top, CancellationToken ct = default)
    {
        top = Math.Clamp(top, 1, 50);

        return db.Materials
            .AsNoTracking()
            .Where(m => m.IsActive && m.SafetyStock > 0)
            .Select(m => new InventorySummaryDto
            {
                MaterialId = m.Id,
                MaterialCode = m.Code,
                MaterialName = m.Name,
                BaseUom = m.BaseUom,
                SafetyStock = m.SafetyStock,
                Quantity = db.Inventories.Where(i => i.MaterialId == m.Id).Sum(i => (decimal?)i.Quantity) ?? 0,
                ReservedQuantity = db.Inventories.Where(i => i.MaterialId == m.Id).Sum(i => (decimal?)i.ReservedQuantity) ?? 0,
                LocationCount = db.Inventories.Count(i => i.MaterialId == m.Id && i.Quantity > 0)
            })
            .Where(s => s.Quantity < s.SafetyStock)
            .OrderBy(s => s.Quantity - s.SafetyStock)
            .Take(top)
            .ToListAsync(ct);
    }
}
