using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IPutawayService
{
    Task<PagedResult<PutawayTaskDto>> QueryAsync(PutawayQuery query, CancellationToken ct = default);
    Task<PutawayTaskDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PutawayTaskDto> AssignAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<PutawayTaskDto> StartAsync(Guid id, CancellationToken ct = default);
    Task<PutawayTaskDto> CompleteAsync(Guid id, PutawayCompleteRequest request, CancellationToken ct = default);
    Task<PutawayTaskDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public class PutawayService(
    IWmsDbContext db,
    IInventoryEngine engine,
    ICurrentUser currentUser,
    IAuditService audit) : IPutawayService
{
    public Task<PagedResult<PutawayTaskDto>> QueryAsync(PutawayQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.PutawayTasks
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, t => t.WarehouseId == query.WarehouseId)
            .WhereIf(query.Status.HasValue, t => t.Status == query.Status)
            .WhereIf(query.AssignedUserId.HasValue, t => t.AssignedUserId == query.AssignedUserId)
            .WhereIf(query.MaterialId.HasValue, t => t.MaterialId == query.MaterialId)
            .WhereIf(!string.IsNullOrEmpty(keyword),
                t => t.TaskNo.ToLower().Contains(keyword)
                     || t.Material.Code.ToLower().Contains(keyword)
                     || t.Material.Name.ToLower().Contains(keyword))
            .OrderBy(t => t.Status)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.CreatedAt);

        return q.ToPagedResultAsync(query, Projection(db), ct);
    }

    public async Task<PutawayTaskDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.PutawayTasks.AsNoTracking().Where(t => t.Id == id).Select(Projection(db)).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的上架任務。");
    }

    public async Task<PutawayTaskDto> AssignAsync(Guid id, Guid userId, CancellationToken ct = default)
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
        await audit.LogAsync("ASSIGN", "PUTAWAY", "PutawayTask", task.Id, task.TaskNo, null, new { userId }, ct);

        return await GetAsync(id, ct);
    }

    public async Task<PutawayTaskDto> StartAsync(Guid id, CancellationToken ct = default)
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
        await audit.LogAsync("START", "PUTAWAY", "PutawayTask", task.Id, task.TaskNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    public async Task<PutawayTaskDto> CompleteAsync(Guid id, PutawayCompleteRequest request, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var task = await db.PutawayTasks
            .Include(t => t.InboundDetail).ThenInclude(d => d.InboundOrder)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的上架任務。");

        if (task.Status is WmsTaskStatus.COMPLETED or WmsTaskStatus.CANCELLED)
        {
            throw AppException.Conflict($"任務狀態為 {task.Status}，無法完成。", "INVALID_STATUS");
        }

        var quantity = request.Quantity ?? task.Quantity;
        if (quantity <= 0 || quantity > task.Quantity)
        {
            throw new AppException($"上架數量必須介於 0 與 {task.Quantity:0.######} 之間。", "INVALID_QUANTITY");
        }

        var targetLocationId = request.TargetLocationId ?? task.TargetLocationId
            ?? throw new AppException("請指定上架的目標儲位。", "TARGET_LOCATION_REQUIRED");

        var target = await db.Locations.FirstOrDefaultAsync(l => l.Id == targetLocationId, ct)
            ?? throw AppException.NotFound("找不到指定的目標儲位。");

        if (target.WarehouseId != task.WarehouseId)
        {
            throw new AppException("目標儲位不屬於此任務的倉庫。", "INVALID_LOCATION");
        }

        if (!target.IsActive)
        {
            throw new AppException($"儲位 {target.Code} 已停用，無法上架。", "LOCATION_INACTIVE");
        }

        if (task.SourceLocationId is null)
        {
            throw new AppException("任務缺少來源儲位，無法上架。", "SOURCE_LOCATION_REQUIRED");
        }

        await engine.TransferAsync(new InventoryTransferContext
        {
            WarehouseId = task.WarehouseId,
            FromLocationId = task.SourceLocationId.Value,
            ToLocationId = target.Id,
            MaterialId = task.MaterialId,
            Quantity = quantity,
            TransactionType = TransactionType.PUTAWAY,
            ReferenceType = ReferenceType.PUTAWAY_TASK,
            ReferenceId = task.Id,
            ReferenceNo = task.TaskNo
        }, ct);

        task.TargetLocationId = target.Id;
        task.Quantity = quantity;
        task.Status = WmsTaskStatus.COMPLETED;
        task.CompletedAt = DateTime.UtcNow;
        task.StartedAt ??= DateTime.UtcNow;
        task.AssignedUserId ??= currentUser.UserId;

        // 同步回寫入庫明細與入庫單的進度。
        var detail = task.InboundDetail;
        detail.PutawayQuantity += quantity;
        if (detail.PutawayQuantity >= detail.ReceivedQuantity)
        {
            detail.Status = InboundDetailStatus.PUTAWAY;
        }

        var order = detail.InboundOrder;
        var allDetails = await db.InboundDetails
            .Where(d => d.InboundOrderId == order.Id)
            .ToListAsync(ct);

        if (allDetails.All(d => d.PutawayQuantity >= d.ReceivedQuantity && d.ReceivedQuantity > 0))
        {
            order.Status = InboundStatus.PUTAWAY;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("COMPLETE", "PUTAWAY", "PutawayTask", task.Id, task.TaskNo,
            null, new { targetLocation = target.Code, quantity }, ct);

        return await GetAsync(id, ct);
    }

    public async Task<PutawayTaskDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetTaskAsync(id, ct);

        if (task.Status == WmsTaskStatus.COMPLETED)
        {
            throw AppException.Conflict("已完成的任務無法取消。", "INVALID_STATUS");
        }

        task.Status = WmsTaskStatus.CANCELLED;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CANCEL", "PUTAWAY", "PutawayTask", task.Id, task.TaskNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    private async Task<PutawayTask> GetTaskAsync(Guid id, CancellationToken ct)
        => await db.PutawayTasks.FirstOrDefaultAsync(t => t.Id == id, ct)
           ?? throw AppException.NotFound("找不到指定的上架任務。");

    private static System.Linq.Expressions.Expression<Func<PutawayTask, PutawayTaskDto>> Projection(IWmsDbContext db) =>
        t => new PutawayTaskDto
        {
            Id = t.Id,
            TaskNo = t.TaskNo,
            InboundDetailId = t.InboundDetailId,
            InboundOrderNo = t.InboundDetail.InboundOrder.OrderNo,
            MaterialId = t.MaterialId,
            MaterialCode = t.Material.Code,
            MaterialName = t.Material.Name,
            BaseUom = t.Material.BaseUom,
            WarehouseId = t.WarehouseId,
            WarehouseCode = db.Warehouses.Where(w => w.Id == t.WarehouseId).Select(w => w.Code).FirstOrDefault() ?? string.Empty,
            Quantity = t.Quantity,
            SourceLocationId = t.SourceLocationId,
            SourceLocationCode = t.SourceLocation != null ? t.SourceLocation.Code : null,
            TargetLocationId = t.TargetLocationId,
            TargetLocationCode = t.TargetLocation != null ? t.TargetLocation.Code : null,
            Status = t.Status,
            Priority = t.Priority,
            AssignedUserId = t.AssignedUserId,
            AssignedUserName = db.Users.Where(u => u.Id == t.AssignedUserId).Select(u => u.DisplayName).FirstOrDefault(),
            StartedAt = t.StartedAt,
            CompletedAt = t.CompletedAt,
            CreatedAt = t.CreatedAt
        };
}
