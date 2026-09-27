using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IPickingService
{
    Task<PagedResult<PickTaskDto>> QueryAsync(PickQuery query, CancellationToken ct = default);
    Task<PickTaskDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PickTaskDto> AssignAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<PickTaskDto> StartAsync(Guid id, CancellationToken ct = default);
    Task<PickTaskDto> CompleteAsync(Guid id, PickCompleteRequest request, CancellationToken ct = default);
    Task<PickTaskDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public class PickingService(
    IWmsDbContext db,
    IInventoryEngine engine,
    ICurrentUser currentUser,
    IAuditService audit) : IPickingService
{
    public Task<PagedResult<PickTaskDto>> QueryAsync(PickQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.PickTasks
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, t => t.WarehouseId == query.WarehouseId)
            .WhereIf(query.Status.HasValue, t => t.Status == query.Status)
            .WhereIf(query.AssignedUserId.HasValue, t => t.AssignedUserId == query.AssignedUserId)
            .WhereIf(query.OutboundOrderId.HasValue, t => t.OutboundDetail.OutboundOrderId == query.OutboundOrderId)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                t => t.TaskNo.ToLower().Contains(keyword)
                     || t.Material.Code.ToLower().Contains(keyword)
                     || t.Material.Name.ToLower().Contains(keyword)
                     || t.SourceLocation.Code.ToLower().Contains(keyword))
            .OrderBy(t => t.Status)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.SourceLocation.Code);

        return q.ToPagedResultAsync(query, Projection(db), ct);
    }

    public async Task<PickTaskDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.PickTasks.AsNoTracking().Where(t => t.Id == id).Select(Projection(db)).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的揀貨任務。");
    }

    public async Task<PickTaskDto> AssignAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskAsync(id, ct);

        if (task.Status is WmsTaskStatus.COMPLETED or WmsTaskStatus.CANCELLED)
        {
            throw AppException.Conflict($"任務狀態為 {task.Status}，無法指派。", "INVALID_STATUS");
        }

        if (!await db.Users.AnyAsync(u => u.Id == userId && u.IsActive, ct))
        {
            throw AppException.NotFound("找不到指定的使用者，或該使用者已停用。");
        }

        task.AssignedUserId = userId;
        task.Status = WmsTaskStatus.ASSIGNED;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("ASSIGN", "PICKING", "PickTask", task.Id, task.TaskNo, null, new { userId }, ct);

        return await GetAsync(id, ct);
    }

    public async Task<PickTaskDto> StartAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetTaskAsync(id, ct);

        if (task.Status is not (WmsTaskStatus.PENDING or WmsTaskStatus.ASSIGNED))
        {
            throw AppException.Conflict($"任務狀態為 {task.Status}，無法開始。", "INVALID_STATUS");
        }

        task.Status = WmsTaskStatus.PROCESSING;
        task.StartedAt = DateTime.UtcNow;
        task.AssignedUserId ??= currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("START", "PICKING", "PickTask", task.Id, task.TaskNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    /// <summary>
    /// 完成揀貨：從來源儲位扣帳並同步消耗預留量。
    /// 少揀的部分會把多餘的預留釋放掉，避免庫存被永久鎖住。
    /// </summary>
    public async Task<PickTaskDto> CompleteAsync(Guid id, PickCompleteRequest request, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var task = await db.PickTasks
            .Include(t => t.OutboundDetail).ThenInclude(d => d.OutboundOrder)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的揀貨任務。");

        if (task.Status is WmsTaskStatus.COMPLETED or WmsTaskStatus.CANCELLED)
        {
            throw AppException.Conflict($"任務狀態為 {task.Status}，無法完成。", "INVALID_STATUS");
        }

        var picked = request.PickedQuantity ?? task.Quantity;
        if (picked <= 0 || picked > task.Quantity)
        {
            throw new AppException($"揀貨數量必須介於 0 與 {task.Quantity:0.######} 之間。", "INVALID_QUANTITY");
        }

        var order = task.OutboundDetail.OutboundOrder;

        await engine.MoveOutAsync(new InventoryMoveContext
        {
            WarehouseId = task.WarehouseId,
            LocationId = task.SourceLocationId,
            MaterialId = task.MaterialId,
            Quantity = picked,
            TransactionType = TransactionType.PICK,
            ReferenceType = ReferenceType.PICK_TASK,
            ReferenceId = task.Id,
            ReferenceNo = task.TaskNo
        }, consumeReservation: true, ct);

        // 短揀：把沒揀到的預留還回去。
        var shortage = task.Quantity - picked;
        if (shortage > 0)
        {
            var inventory = await engine.FindAsync(task.SourceLocationId, task.MaterialId, ct: ct);
            if (inventory is not null)
            {
                await engine.ReleaseAsync(inventory, shortage, ReferenceType.OUTBOUND_ORDER, order.Id, order.OrderNo, ct);
            }
        }

        task.PickedQuantity = picked;
        task.Status = WmsTaskStatus.COMPLETED;
        task.CompletedAt = DateTime.UtcNow;
        task.StartedAt ??= DateTime.UtcNow;
        task.AssignedUserId ??= currentUser.UserId;

        var detail = task.OutboundDetail;
        detail.PickedQuantity += picked;
        if (shortage > 0)
        {
            detail.AllocatedQuantity -= shortage;
        }

        detail.Status = detail.PickedQuantity >= detail.RequestedQuantity
            ? OutboundDetailStatus.PICKED
            : OutboundDetailStatus.PICKING;

        // 整張出庫單的所有揀貨任務都結束時，把單據推進到 PICKED。
        var detailIds = await db.OutboundDetails
            .Where(d => d.OutboundOrderId == order.Id)
            .Select(d => d.Id)
            .ToListAsync(ct);

        var stillOpen = await db.PickTasks
            .CountAsync(t => detailIds.Contains(t.OutboundDetailId)
                             && t.Id != task.Id
                             && t.Status != WmsTaskStatus.COMPLETED
                             && t.Status != WmsTaskStatus.CANCELLED, ct);

        order.Status = stillOpen == 0 ? OutboundStatus.PICKED : OutboundStatus.PICKING;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("COMPLETE", "PICKING", "PickTask", task.Id, task.TaskNo, null, new { picked }, ct);

        return await GetAsync(id, ct);
    }

    public async Task<PickTaskDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var task = await db.PickTasks
            .Include(t => t.OutboundDetail).ThenInclude(d => d.OutboundOrder)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的揀貨任務。");

        if (task.Status == WmsTaskStatus.COMPLETED)
        {
            throw AppException.Conflict("已完成的任務無法取消。", "INVALID_STATUS");
        }

        var order = task.OutboundDetail.OutboundOrder;
        var remaining = task.Quantity - task.PickedQuantity;

        if (remaining > 0)
        {
            var inventory = await engine.FindAsync(task.SourceLocationId, task.MaterialId, ct: ct);
            if (inventory is not null)
            {
                await engine.ReleaseAsync(inventory, remaining, ReferenceType.OUTBOUND_ORDER, order.Id, order.OrderNo, ct);
            }
        }

        task.OutboundDetail.AllocatedQuantity -= remaining;
        task.Status = WmsTaskStatus.CANCELLED;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("CANCEL", "PICKING", "PickTask", task.Id, task.TaskNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    private async Task<PickTask> GetTaskAsync(Guid id, CancellationToken ct)
        => await db.PickTasks.FirstOrDefaultAsync(t => t.Id == id, ct)
           ?? throw AppException.NotFound("找不到指定的揀貨任務。");

    private static System.Linq.Expressions.Expression<Func<PickTask, PickTaskDto>> Projection(IWmsDbContext db) =>
        t => new PickTaskDto
        {
            Id = t.Id,
            TaskNo = t.TaskNo,
            OutboundDetailId = t.OutboundDetailId,
            OutboundOrderNo = t.OutboundDetail.OutboundOrder.OrderNo,
            MaterialId = t.MaterialId,
            MaterialCode = t.Material.Code,
            MaterialName = t.Material.Name,
            BaseUom = t.Material.BaseUom,
            WarehouseId = t.WarehouseId,
            WarehouseCode = db.Warehouses.Where(w => w.Id == t.WarehouseId).Select(w => w.Code).FirstOrDefault() ?? string.Empty,
            SourceLocationId = t.SourceLocationId,
            SourceLocationCode = t.SourceLocation.Code,
            Quantity = t.Quantity,
            PickedQuantity = t.PickedQuantity,
            Status = t.Status,
            Priority = t.Priority,
            AssignedUserId = t.AssignedUserId,
            AssignedUserName = db.Users.Where(u => u.Id == t.AssignedUserId).Select(u => u.DisplayName).FirstOrDefault(),
            StartedAt = t.StartedAt,
            CompletedAt = t.CompletedAt,
            CreatedAt = t.CreatedAt
        };
}
