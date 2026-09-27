using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IStocktakeService
{
    Task<PagedResult<StocktakeDto>> QueryAsync(StocktakeQuery query, CancellationToken ct = default);
    Task<StocktakeDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<StocktakeDto> CreateAsync(StocktakeCreateDto dto, CancellationToken ct = default);
    Task<StocktakeDto> StartAsync(Guid id, CancellationToken ct = default);
    Task<StocktakeDto> CountAsync(Guid id, StocktakeCountRequest request, CancellationToken ct = default);
    Task<StocktakeDto> RecountAsync(Guid id, StocktakeRecountRequest request, CancellationToken ct = default);
    Task<StocktakeDto> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<StocktakeDto> ApproveAsync(Guid id, CancellationToken ct = default);
    Task<StocktakeDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public class StocktakeService(
    IWmsDbContext db,
    IInventoryEngine engine,
    INumberGenerator numberGenerator,
    ICurrentUser currentUser,
    IAuditService audit) : IStocktakeService
{
    public Task<PagedResult<StocktakeDto>> QueryAsync(StocktakeQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Stocktakes
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, s => s.WarehouseId == query.WarehouseId)
            .WhereIf(query.Status.HasValue, s => s.Status == query.Status)
            .WhereIf(query.DateFrom.HasValue, s => s.CreatedAt >= query.DateFrom!.Value)
            .WhereIf(query.DateTo.HasValue, s => s.CreatedAt < query.DateTo!.Value.AddDays(1))
            .WhereIf(!string.IsNullOrEmpty(keyword), s => s.StocktakeNo.ToLower().Contains(keyword))
            .OrderByDescending(s => s.CreatedAt);

        return q.ToPagedResultAsync(query, ListProjection(db), ct);
    }

    public async Task<StocktakeDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var stocktake = await db.Stocktakes
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Zone)
            .Include(s => s.Details).ThenInclude(d => d.Material)
            .Include(s => s.Details).ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的盤點單。");

        var userNames = await db.Users
            .Where(u => u.Id == stocktake.CreatedBy || u.Id == stocktake.ApprovedBy)
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var counterIds = stocktake.Details.Where(d => d.CountedBy.HasValue).Select(d => d.CountedBy!.Value).Distinct().ToList();
        var counters = await db.Users
            .Where(u => counterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        return new StocktakeDto
        {
            Id = stocktake.Id,
            StocktakeNo = stocktake.StocktakeNo,
            WarehouseId = stocktake.WarehouseId,
            WarehouseCode = stocktake.Warehouse.Code,
            WarehouseName = stocktake.Warehouse.Name,
            ZoneId = stocktake.ZoneId,
            ZoneCode = stocktake.Zone?.Code,
            Status = stocktake.Status,
            Remark = stocktake.Remark,
            StartedAt = stocktake.StartedAt,
            CompletedAt = stocktake.CompletedAt,
            CreatedByName = stocktake.CreatedBy.HasValue && userNames.TryGetValue(stocktake.CreatedBy.Value, out var c) ? c : null,
            ApprovedByName = stocktake.ApprovedBy.HasValue && userNames.TryGetValue(stocktake.ApprovedBy.Value, out var a) ? a : null,
            ApprovedAt = stocktake.ApprovedAt,
            CreatedAt = stocktake.CreatedAt,
            LineCount = stocktake.Details.Count,
            CountedCount = stocktake.Details.Count(d => d.CountedQuantity.HasValue),
            DifferenceCount = stocktake.Details.Count(d => d.CountedQuantity.HasValue && d.DifferenceQuantity != 0),
            Details = [.. stocktake.Details
                .OrderBy(d => d.Location.Code).ThenBy(d => d.Material.Code)
                .Select(d => new StocktakeDetailDto
                {
                    Id = d.Id,
                    LocationId = d.LocationId,
                    LocationCode = d.Location.Code,
                    MaterialId = d.MaterialId,
                    MaterialCode = d.Material.Code,
                    MaterialName = d.Material.Name,
                    BaseUom = d.Material.BaseUom,
                    SystemQuantity = d.SystemQuantity,
                    CountedQuantity = d.CountedQuantity,
                    DifferenceQuantity = d.DifferenceQuantity,
                    Status = d.Status,
                    CountedByName = d.CountedBy.HasValue && counters.TryGetValue(d.CountedBy.Value, out var n) ? n : null,
                    CountedAt = d.CountedAt,
                    Remark = d.Remark
                })]
        };
    }

    /// <summary>建立盤點單並 Snapshot 目前的系統庫存。</summary>
    public async Task<StocktakeDto> CreateAsync(StocktakeCreateDto dto, CancellationToken ct = default)
    {
        if (!await db.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId, ct))
        {
            throw AppException.NotFound("找不到指定的倉庫。");
        }

        var inProgress = await db.Stocktakes.AnyAsync(
            s => s.WarehouseId == dto.WarehouseId
                 && (s.Status == StocktakeStatus.COUNTING || s.Status == StocktakeStatus.RECOUNT),
            ct);

        if (inProgress)
        {
            throw AppException.Conflict("此倉庫已有進行中的盤點單，請先結束後再建立。", "STOCKTAKE_IN_PROGRESS");
        }

        var snapshot = await db.Inventories
            .AsNoTracking()
            .Where(i => i.WarehouseId == dto.WarehouseId && i.Quantity != 0)
            .WhereIf(dto.ZoneId.HasValue, i => i.Location.ZoneId == dto.ZoneId)
            .Select(i => new { i.LocationId, i.MaterialId, i.Quantity })
            .ToListAsync(ct);

        if (snapshot.Count == 0)
        {
            throw new AppException("指定範圍內沒有任何庫存，無法建立盤點單。", "NO_INVENTORY");
        }

        var stocktake = new Stocktake
        {
            StocktakeNo = await numberGenerator.NextAsync("ST", ct),
            WarehouseId = dto.WarehouseId,
            ZoneId = dto.ZoneId,
            Status = StocktakeStatus.DRAFT,
            Remark = dto.Remark,
            CreatedBy = currentUser.UserId
        };

        foreach (var row in snapshot)
        {
            stocktake.Details.Add(new StocktakeDetail
            {
                LocationId = row.LocationId,
                MaterialId = row.MaterialId,
                SystemQuantity = row.Quantity,
                Status = StocktakeDetailStatus.PENDING
            });
        }

        db.Stocktakes.Add(stocktake);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "STOCKTAKE", "Stocktake", stocktake.Id, stocktake.StocktakeNo, null, dto, ct);

        return await GetAsync(stocktake.Id, ct);
    }

    public async Task<StocktakeDto> StartAsync(Guid id, CancellationToken ct = default)
    {
        var stocktake = await GetEntityAsync(id, ct);

        if (stocktake.Status != StocktakeStatus.DRAFT)
        {
            throw AppException.Conflict($"盤點單狀態為 {stocktake.Status}，無法開始。", "INVALID_STATUS");
        }

        stocktake.Status = StocktakeStatus.COUNTING;
        stocktake.StartedAt = DateTime.UtcNow;
        stocktake.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("START", "STOCKTAKE", "Stocktake", id, stocktake.StocktakeNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    public async Task<StocktakeDto> CountAsync(Guid id, StocktakeCountRequest request, CancellationToken ct = default)
    {
        var stocktake = await db.Stocktakes
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的盤點單。");

        if (stocktake.Status is not (StocktakeStatus.COUNTING or StocktakeStatus.RECOUNT))
        {
            throw AppException.Conflict($"盤點單狀態為 {stocktake.Status}，無法輸入盤點數。", "INVALID_STATUS");
        }

        foreach (var line in request.Lines)
        {
            var detail = stocktake.Details.FirstOrDefault(d => d.Id == line.DetailId)
                ?? throw AppException.NotFound($"盤點單中找不到明細 {line.DetailId}。");

            detail.CountedQuantity = line.CountedQuantity;
            detail.DifferenceQuantity = line.CountedQuantity - detail.SystemQuantity;
            detail.Status = detail.DifferenceQuantity == 0
                ? StocktakeDetailStatus.COUNTED
                : StocktakeDetailStatus.DIFFERENCE;
            detail.CountedBy = currentUser.UserId;
            detail.CountedAt = DateTime.UtcNow;
            detail.Remark = line.Remark;
        }

        stocktake.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("COUNT", "STOCKTAKE", "Stocktake", id, stocktake.StocktakeNo,
            null, new { lineCount = request.Lines.Count }, ct);

        return await GetAsync(id, ct);
    }

    public async Task<StocktakeDto> RecountAsync(Guid id, StocktakeRecountRequest request, CancellationToken ct = default)
    {
        var stocktake = await db.Stocktakes
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的盤點單。");

        if (stocktake.Status is StocktakeStatus.APPROVED or StocktakeStatus.CANCELLED)
        {
            throw AppException.Conflict($"盤點單狀態為 {stocktake.Status}，無法複盤。", "INVALID_STATUS");
        }

        foreach (var detailId in request.DetailIds)
        {
            var detail = stocktake.Details.FirstOrDefault(d => d.Id == detailId)
                ?? throw AppException.NotFound($"盤點單中找不到明細 {detailId}。");

            // 複盤前重新抓一次系統數量，避免拿到過期的帳面值。
            var inventory = await engine.FindAsync(detail.LocationId, detail.MaterialId, ct: ct);
            detail.SystemQuantity = inventory?.Quantity ?? 0;
            detail.CountedQuantity = null;
            detail.DifferenceQuantity = 0;
            detail.Status = StocktakeDetailStatus.RECOUNT;
            detail.CountedBy = null;
            detail.CountedAt = null;
        }

        stocktake.Status = StocktakeStatus.RECOUNT;
        stocktake.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("RECOUNT", "STOCKTAKE", "Stocktake", id, stocktake.StocktakeNo,
            null, new { count = request.DetailIds.Count }, ct);

        return await GetAsync(id, ct);
    }

    public async Task<StocktakeDto> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var stocktake = await db.Stocktakes
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的盤點單。");

        if (stocktake.Status is not (StocktakeStatus.COUNTING or StocktakeStatus.RECOUNT))
        {
            throw AppException.Conflict($"盤點單狀態為 {stocktake.Status}，無法結束盤點。", "INVALID_STATUS");
        }

        var uncounted = stocktake.Details.Count(d => !d.CountedQuantity.HasValue);
        if (uncounted > 0)
        {
            throw AppException.Conflict($"尚有 {uncounted} 筆明細未輸入盤點數。", "NOT_COUNTED");
        }

        stocktake.Status = StocktakeStatus.PENDING_APPROVAL;
        stocktake.CompletedAt = DateTime.UtcNow;
        stocktake.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("COMPLETE", "STOCKTAKE", "Stocktake", id, stocktake.StocktakeNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    /// <summary>核准盤點：依差異調整庫存，每一筆差異都會留下 STOCKTAKE 異動。</summary>
    public async Task<StocktakeDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var stocktake = await db.Stocktakes
            .Include(s => s.Details).ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的盤點單。");

        if (stocktake.Status != StocktakeStatus.PENDING_APPROVAL)
        {
            throw AppException.Conflict($"盤點單狀態為 {stocktake.Status}，無法核准。", "INVALID_STATUS");
        }

        foreach (var detail in stocktake.Details.Where(d => d.DifferenceQuantity != 0))
        {
            var context = new InventoryMoveContext
            {
                WarehouseId = stocktake.WarehouseId,
                LocationId = detail.LocationId,
                MaterialId = detail.MaterialId,
                Quantity = Math.Abs(detail.DifferenceQuantity),
                TransactionType = TransactionType.STOCKTAKE,
                ReferenceType = ReferenceType.STOCKTAKE,
                ReferenceId = stocktake.Id,
                ReferenceNo = stocktake.StocktakeNo,
                Remark = $"盤點差異調整（帳面 {detail.SystemQuantity:0.######} → 實盤 {detail.CountedQuantity:0.######}）"
            };

            if (detail.DifferenceQuantity > 0)
            {
                await engine.MoveInAsync(context, ct);
            }
            else
            {
                await engine.MoveOutAsync(context, consumeReservation: false, ct);
            }

            detail.Status = StocktakeDetailStatus.ADJUSTED;
        }

        stocktake.Status = StocktakeStatus.APPROVED;
        stocktake.ApprovedBy = currentUser.UserId;
        stocktake.ApprovedAt = DateTime.UtcNow;
        stocktake.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("APPROVE", "STOCKTAKE", "Stocktake", id, stocktake.StocktakeNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    public async Task<StocktakeDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var stocktake = await GetEntityAsync(id, ct);

        if (stocktake.Status == StocktakeStatus.APPROVED)
        {
            throw AppException.Conflict("已核准的盤點單無法取消。", "INVALID_STATUS");
        }

        stocktake.Status = StocktakeStatus.CANCELLED;
        stocktake.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CANCEL", "STOCKTAKE", "Stocktake", id, stocktake.StocktakeNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    private async Task<Stocktake> GetEntityAsync(Guid id, CancellationToken ct)
        => await db.Stocktakes.FirstOrDefaultAsync(s => s.Id == id, ct)
           ?? throw AppException.NotFound("找不到指定的盤點單。");

    private static System.Linq.Expressions.Expression<Func<Stocktake, StocktakeDto>> ListProjection(IWmsDbContext db) =>
        s => new StocktakeDto
        {
            Id = s.Id,
            StocktakeNo = s.StocktakeNo,
            WarehouseId = s.WarehouseId,
            WarehouseCode = s.Warehouse.Code,
            WarehouseName = s.Warehouse.Name,
            ZoneId = s.ZoneId,
            ZoneCode = s.Zone != null ? s.Zone.Code : null,
            Status = s.Status,
            Remark = s.Remark,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            CreatedByName = db.Users.Where(u => u.Id == s.CreatedBy).Select(u => u.DisplayName).FirstOrDefault(),
            ApprovedByName = db.Users.Where(u => u.Id == s.ApprovedBy).Select(u => u.DisplayName).FirstOrDefault(),
            ApprovedAt = s.ApprovedAt,
            CreatedAt = s.CreatedAt,
            LineCount = s.Details.Count,
            CountedCount = s.Details.Count(d => d.CountedQuantity != null),
            DifferenceCount = s.Details.Count(d => d.CountedQuantity != null && d.DifferenceQuantity != 0)
        };
}
