using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface ITransferService
{
    Task<PagedResult<TransferOrderDto>> QueryAsync(TransferQuery query, CancellationToken ct = default);
    Task<TransferOrderDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<TransferOrderDto> CreateAsync(TransferCreateDto dto, CancellationToken ct = default);
    Task<TransferOrderDto> ExecuteAsync(Guid id, CancellationToken ct = default);
    Task<TransferOrderDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public class TransferService(
    IWmsDbContext db,
    IInventoryEngine engine,
    INumberGenerator numberGenerator,
    ICurrentUser currentUser,
    IAuditService audit) : ITransferService
{
    public Task<PagedResult<TransferOrderDto>> QueryAsync(TransferQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.TransferOrders
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, t => t.WarehouseId == query.WarehouseId)
            .WhereIf(query.MaterialId.HasValue, t => t.MaterialId == query.MaterialId)
            .WhereIf(query.Status.HasValue, t => t.Status == query.Status)
            .WhereIf(query.DateFrom.HasValue, t => t.CreatedAt >= query.DateFrom!.Value)
            .WhereIf(query.DateTo.HasValue, t => t.CreatedAt < query.DateTo!.Value.AddDays(1))
            .WhereIf(!string.IsNullOrEmpty(keyword),
                t => t.TransferNo.ToLower().Contains(keyword)
                     || t.Material.Code.ToLower().Contains(keyword)
                     || t.FromLocation.Code.ToLower().Contains(keyword)
                     || t.ToLocation.Code.ToLower().Contains(keyword))
            .OrderByDescending(t => t.CreatedAt);

        return q.ToPagedResultAsync(query, Projection(db), ct);
    }

    public async Task<TransferOrderDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.TransferOrders.AsNoTracking().Where(t => t.Id == id).Select(Projection(db)).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的移庫單。");
    }

    public async Task<TransferOrderDto> CreateAsync(TransferCreateDto dto, CancellationToken ct = default)
    {
        var from = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == dto.FromLocationId, ct)
            ?? throw AppException.NotFound("找不到來源儲位。");

        var to = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == dto.ToLocationId, ct)
            ?? throw AppException.NotFound("找不到目的儲位。");

        if (from.Id == to.Id)
        {
            throw new AppException("來源儲位與目的儲位不可相同。", "INVALID_LOCATION");
        }

        if (from.WarehouseId != to.WarehouseId)
        {
            throw new AppException("v1.0 僅支援同一倉庫內的移庫。", "CROSS_WAREHOUSE");
        }

        if (!to.IsActive)
        {
            throw new AppException($"目的儲位 {to.Code} 已停用。", "LOCATION_INACTIVE");
        }

        if (!await db.Materials.AnyAsync(m => m.Id == dto.MaterialId, ct))
        {
            throw AppException.NotFound("找不到指定的物料。");
        }

        // 建立階段先確認來源可用量，避免產生註定失敗的移庫單。
        var source = await engine.FindAsync(from.Id, dto.MaterialId, ct: ct)
            ?? throw AppException.InsufficientStock($"來源儲位 {from.Code} 沒有此物料的庫存。");

        var available = source.Quantity - source.ReservedQuantity;
        if (available < dto.Quantity)
        {
            throw AppException.InsufficientStock(
                $"來源儲位可用庫存不足：可用 {available:0.######}，需求 {dto.Quantity:0.######}");
        }

        var order = new TransferOrder
        {
            TransferNo = await numberGenerator.NextAsync("TR", ct),
            WarehouseId = from.WarehouseId,
            MaterialId = dto.MaterialId,
            FromLocationId = from.Id,
            ToLocationId = to.Id,
            Quantity = dto.Quantity,
            Status = TransferStatus.PENDING,
            Reason = dto.Reason,
            Remark = dto.Remark,
            CreatedBy = currentUser.UserId
        };

        db.TransferOrders.Add(order);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "TRANSFER", "TransferOrder", order.Id, order.TransferNo, null, dto, ct);

        return dto.ExecuteImmediately
            ? await ExecuteAsync(order.Id, ct)
            : await GetAsync(order.Id, ct);
    }

    public async Task<TransferOrderDto> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.TransferOrders.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的移庫單。");

        if (order.Status != TransferStatus.PENDING && order.Status != TransferStatus.DRAFT)
        {
            throw AppException.Conflict($"移庫單狀態為 {order.Status}，無法執行。", "INVALID_STATUS");
        }

        await engine.TransferAsync(new InventoryTransferContext
        {
            WarehouseId = order.WarehouseId,
            FromLocationId = order.FromLocationId,
            ToLocationId = order.ToLocationId,
            MaterialId = order.MaterialId,
            Quantity = order.Quantity,
            TransactionType = TransactionType.TRANSFER,
            ReferenceType = ReferenceType.TRANSFER_ORDER,
            ReferenceId = order.Id,
            ReferenceNo = order.TransferNo,
            Remark = order.Reason
        }, ct);

        order.Status = TransferStatus.COMPLETED;
        order.ExecutedBy = currentUser.UserId;
        order.ExecutedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("EXECUTE", "TRANSFER", "TransferOrder", order.Id, order.TransferNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    public async Task<TransferOrderDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.TransferOrders.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的移庫單。");

        if (order.Status == TransferStatus.COMPLETED)
        {
            throw AppException.Conflict("已完成的移庫單無法取消。", "INVALID_STATUS");
        }

        order.Status = TransferStatus.CANCELLED;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CANCEL", "TRANSFER", "TransferOrder", order.Id, order.TransferNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    private static System.Linq.Expressions.Expression<Func<TransferOrder, TransferOrderDto>> Projection(IWmsDbContext db) =>
        t => new TransferOrderDto
        {
            Id = t.Id,
            TransferNo = t.TransferNo,
            WarehouseId = t.WarehouseId,
            WarehouseCode = t.Warehouse.Code,
            MaterialId = t.MaterialId,
            MaterialCode = t.Material.Code,
            MaterialName = t.Material.Name,
            BaseUom = t.Material.BaseUom,
            FromLocationId = t.FromLocationId,
            FromLocationCode = t.FromLocation.Code,
            ToLocationId = t.ToLocationId,
            ToLocationCode = t.ToLocation.Code,
            Quantity = t.Quantity,
            Status = t.Status,
            Reason = t.Reason,
            Remark = t.Remark,
            CreatedByName = db.Users.Where(u => u.Id == t.CreatedBy).Select(u => u.DisplayName).FirstOrDefault(),
            ExecutedByName = db.Users.Where(u => u.Id == t.ExecutedBy).Select(u => u.DisplayName).FirstOrDefault(),
            ExecutedAt = t.ExecutedAt,
            CreatedAt = t.CreatedAt
        };
}
