using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IOutboundService
{
    Task<PagedResult<OutboundOrderDto>> QueryAsync(OutboundQuery query, CancellationToken ct = default);
    Task<OutboundOrderDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<OutboundOrderDto> CreateAsync(OutboundOrderCreateDto dto, CancellationToken ct = default);
    Task<AllocationResultDto> AllocateAsync(Guid id, CancellationToken ct = default);
    Task<OutboundOrderDto> ReleaseAsync(Guid id, CancellationToken ct = default);
    Task<OutboundOrderDto> ShipAsync(Guid id, ShipRequest request, CancellationToken ct = default);
    Task<OutboundOrderDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public class OutboundService(
    IWmsDbContext db,
    IInventoryEngine engine,
    INumberGenerator numberGenerator,
    ICurrentUser currentUser,
    IAuditService audit) : IOutboundService
{
    public Task<PagedResult<OutboundOrderDto>> QueryAsync(OutboundQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.OutboundOrders
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, o => o.WarehouseId == query.WarehouseId)
            .WhereIf(query.Status.HasValue, o => o.Status == query.Status)
            .WhereIf(query.DateFrom.HasValue, o => o.CreatedAt >= query.DateFrom!.Value)
            .WhereIf(query.DateTo.HasValue, o => o.CreatedAt < query.DateTo!.Value.AddDays(1))
            .WhereIf(!string.IsNullOrEmpty(keyword),
                o => o.OrderNo.ToLower().Contains(keyword)
                     || (o.ExternalOrderNo != null && o.ExternalOrderNo.ToLower().Contains(keyword))
                     || (o.CustomerName != null && o.CustomerName.ToLower().Contains(keyword)))
            .OrderByDescending(o => o.CreatedAt);

        return q.ToPagedResultAsync(query, o => new OutboundOrderDto
        {
            Id = o.Id,
            OrderNo = o.OrderNo,
            ExternalOrderNo = o.ExternalOrderNo,
            CustomerCode = o.CustomerCode,
            CustomerName = o.CustomerName,
            WarehouseId = o.WarehouseId,
            WarehouseCode = o.Warehouse.Code,
            WarehouseName = o.Warehouse.Name,
            Status = o.Status,
            Priority = o.Priority,
            RequestedShipDate = o.RequestedShipDate,
            ShippedAt = o.ShippedAt,
            Remark = o.Remark,
            CreatedByName = db.Users.Where(u => u.Id == o.CreatedBy).Select(u => u.DisplayName).FirstOrDefault(),
            CreatedAt = o.CreatedAt,
            LineCount = o.Details.Count,
            TotalRequestedQuantity = o.Details.Sum(d => (decimal?)d.RequestedQuantity) ?? 0,
            TotalAllocatedQuantity = o.Details.Sum(d => (decimal?)d.AllocatedQuantity) ?? 0,
            TotalPickedQuantity = o.Details.Sum(d => (decimal?)d.PickedQuantity) ?? 0
        }, ct);
    }

    public async Task<OutboundOrderDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.OutboundOrders
            .AsNoTracking()
            .Include(o => o.Warehouse)
            .Include(o => o.Details).ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的出庫單。");

        var createdByName = await db.Users
            .Where(u => u.Id == order.CreatedBy)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct);

        return new OutboundOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            ExternalOrderNo = order.ExternalOrderNo,
            CustomerCode = order.CustomerCode,
            CustomerName = order.CustomerName,
            WarehouseId = order.WarehouseId,
            WarehouseCode = order.Warehouse.Code,
            WarehouseName = order.Warehouse.Name,
            Status = order.Status,
            Priority = order.Priority,
            RequestedShipDate = order.RequestedShipDate,
            ShippedAt = order.ShippedAt,
            Remark = order.Remark,
            CreatedByName = createdByName,
            CreatedAt = order.CreatedAt,
            LineCount = order.Details.Count,
            TotalRequestedQuantity = order.Details.Sum(d => d.RequestedQuantity),
            TotalAllocatedQuantity = order.Details.Sum(d => d.AllocatedQuantity),
            TotalPickedQuantity = order.Details.Sum(d => d.PickedQuantity),
            Details = [.. order.Details.OrderBy(d => d.LineNo).Select(d => new OutboundDetailDto
            {
                Id = d.Id,
                LineNo = d.LineNo,
                MaterialId = d.MaterialId,
                MaterialCode = d.Material.Code,
                MaterialName = d.Material.Name,
                BaseUom = d.Material.BaseUom,
                RequestedQuantity = d.RequestedQuantity,
                AllocatedQuantity = d.AllocatedQuantity,
                PickedQuantity = d.PickedQuantity,
                ShippedQuantity = d.ShippedQuantity,
                Status = d.Status,
                Remark = d.Remark
            })]
        };
    }

    public async Task<OutboundOrderDto> CreateAsync(OutboundOrderCreateDto dto, CancellationToken ct = default)
    {
        if (!await db.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId && w.IsActive, ct))
        {
            throw AppException.NotFound("找不到指定的倉庫，或該倉庫已停用。");
        }

        var materialIds = dto.Details.Select(d => d.MaterialId).Distinct().ToList();
        var materials = await db.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        foreach (var materialId in materialIds)
        {
            if (!materials.TryGetValue(materialId, out var material))
            {
                throw AppException.NotFound($"找不到物料 {materialId}。");
            }

            if (!material.IsActive)
            {
                throw new AppException($"物料 {material.Code} 已停用，無法建立出庫單。", "MATERIAL_INACTIVE");
            }
        }

        var order = new OutboundOrder
        {
            OrderNo = await numberGenerator.NextAsync("OB", ct),
            ExternalOrderNo = dto.ExternalOrderNo,
            CustomerCode = dto.CustomerCode,
            CustomerName = dto.CustomerName,
            WarehouseId = dto.WarehouseId,
            Status = OutboundStatus.DRAFT,
            Priority = dto.Priority,
            RequestedShipDate = dto.RequestedShipDate,
            Remark = dto.Remark,
            CreatedBy = currentUser.UserId
        };

        var lineNo = 1;
        foreach (var line in dto.Details)
        {
            order.Details.Add(new OutboundDetail
            {
                LineNo = lineNo++,
                MaterialId = line.MaterialId,
                RequestedQuantity = line.RequestedQuantity,
                Status = OutboundDetailStatus.PENDING,
                Remark = line.Remark
            });
        }

        db.OutboundOrders.Add(order);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "OUTBOUND", "OutboundOrder", order.Id, order.OrderNo, null, dto, ct);

        return await GetAsync(order.Id, ct);
    }

    /// <summary>
    /// 庫存分配：依可用量預留庫存並產生揀貨任務。
    /// 整段包在一個交易內，搭配 Inventory 的樂觀鎖與資料庫檢查條件，防止兩張單同時吃掉同一批庫存。
    /// </summary>
    public async Task<AllocationResultDto> AllocateAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.OutboundOrders
            .Include(o => o.Details).ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的出庫單。");

        if (order.Status is not (OutboundStatus.DRAFT or OutboundStatus.ALLOCATED))
        {
            throw AppException.Conflict($"出庫單狀態為 {order.Status}，無法分配庫存。", "INVALID_STATUS");
        }

        var result = new AllocationResultDto
        {
            OutboundOrderId = order.Id,
            OrderNo = order.OrderNo
        };

        foreach (var detail in order.Details.Where(d => d.Status != OutboundDetailStatus.CANCELLED))
        {
            var required = detail.RequestedQuantity - detail.AllocatedQuantity;
            var line = new AllocationLineDto
            {
                OutboundDetailId = detail.Id,
                MaterialCode = detail.Material.Code,
                RequestedQuantity = detail.RequestedQuantity
            };

            if (required <= 0)
            {
                line.AllocatedQuantity = detail.AllocatedQuantity;
                result.Lines.Add(line);
                continue;
            }

            var candidates = await engine.FindAllocatableAsync(order.WarehouseId, detail.MaterialId, ct);
            var allocatedThisRound = 0m;

            foreach (var inventory in candidates)
            {
                if (required <= 0)
                {
                    break;
                }

                var available = inventory.Quantity - inventory.ReservedQuantity;
                if (available <= 0)
                {
                    continue;
                }

                var take = Math.Min(available, required);

                await engine.ReserveAsync(inventory, take, ReferenceType.OUTBOUND_ORDER, order.Id, order.OrderNo, ct);

                db.PickTasks.Add(new PickTask
                {
                    TaskNo = await numberGenerator.NextAsync("PK", ct),
                    OutboundDetailId = detail.Id,
                    MaterialId = detail.MaterialId,
                    WarehouseId = order.WarehouseId,
                    SourceLocationId = inventory.LocationId,
                    Quantity = take,
                    Status = WmsTaskStatus.PENDING,
                    Priority = order.Priority
                });

                result.CreatedPickTaskCount++;
                allocatedThisRound += take;
                required -= take;
            }

            detail.AllocatedQuantity += allocatedThisRound;
            detail.Status = detail.AllocatedQuantity >= detail.RequestedQuantity
                ? OutboundDetailStatus.ALLOCATED
                : detail.AllocatedQuantity > 0
                    ? OutboundDetailStatus.PARTIAL_ALLOCATED
                    : OutboundDetailStatus.PENDING;

            line.AllocatedQuantity = detail.AllocatedQuantity;
            line.ShortageQuantity = detail.RequestedQuantity - detail.AllocatedQuantity;
            result.Lines.Add(line);
        }

        var hasAllocation = order.Details.Any(d => d.AllocatedQuantity > 0);
        if (!hasAllocation)
        {
            throw AppException.InsufficientStock("所有明細皆無可用庫存，無法分配。");
        }

        order.Status = OutboundStatus.ALLOCATED;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        result.Status = order.Status;
        result.FullyAllocated = result.Lines.All(l => l.ShortageQuantity <= 0);

        await audit.LogAsync("ALLOCATE", "OUTBOUND", "OutboundOrder", order.Id, order.OrderNo, null, result, ct);

        return result;
    }

    /// <summary>取消分配：釋放尚未揀出的預留量，並取消對應的揀貨任務。</summary>
    public async Task<OutboundOrderDto> ReleaseAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.OutboundOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的出庫單。");

        if (order.Status is OutboundStatus.SHIPPED or OutboundStatus.CANCELLED)
        {
            throw AppException.Conflict($"出庫單狀態為 {order.Status}，無法取消分配。", "INVALID_STATUS");
        }

        var detailIds = order.Details.Select(d => d.Id).ToList();

        var openTasks = await db.PickTasks
            .Where(t => detailIds.Contains(t.OutboundDetailId)
                        && t.Status != WmsTaskStatus.COMPLETED
                        && t.Status != WmsTaskStatus.CANCELLED)
            .ToListAsync(ct);

        foreach (var task in openTasks)
        {
            var remaining = task.Quantity - task.PickedQuantity;
            if (remaining > 0)
            {
                var inventory = await engine.FindAsync(task.SourceLocationId, task.MaterialId, ct: ct);
                if (inventory is not null)
                {
                    await engine.ReleaseAsync(inventory, remaining, ReferenceType.OUTBOUND_ORDER, order.Id, order.OrderNo, ct);
                }
            }

            task.Status = WmsTaskStatus.CANCELLED;
        }

        foreach (var detail in order.Details)
        {
            detail.AllocatedQuantity = detail.PickedQuantity;
            detail.Status = detail.PickedQuantity > 0
                ? OutboundDetailStatus.PICKED
                : OutboundDetailStatus.PENDING;
        }

        order.Status = order.Details.Any(d => d.PickedQuantity > 0)
            ? OutboundStatus.PICKED
            : OutboundStatus.DRAFT;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("RELEASE", "OUTBOUND", "OutboundOrder", order.Id, order.OrderNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    /// <summary>出貨：揀貨階段已把庫存扣掉，這裡留下 SHIP 憑證並結案。</summary>
    public async Task<OutboundOrderDto> ShipAsync(Guid id, ShipRequest request, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.OutboundOrders
            .Include(o => o.Details).ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的出庫單。");

        if (order.Status is OutboundStatus.SHIPPED or OutboundStatus.CANCELLED)
        {
            throw AppException.Conflict($"出庫單狀態為 {order.Status}，無法出貨。", "INVALID_STATUS");
        }

        if (order.Details.All(d => d.PickedQuantity <= 0))
        {
            throw AppException.Conflict("尚未揀貨，無法出貨。", "NOT_PICKED");
        }

        var detailIds = order.Details.Select(d => d.Id).ToList();
        var openTasks = await db.PickTasks
            .CountAsync(t => detailIds.Contains(t.OutboundDetailId)
                             && t.Status != WmsTaskStatus.COMPLETED
                             && t.Status != WmsTaskStatus.CANCELLED, ct);

        if (openTasks > 0)
        {
            throw AppException.Conflict($"尚有 {openTasks} 筆揀貨任務未完成，無法出貨。", "TASK_PENDING");
        }

        foreach (var detail in order.Details.Where(d => d.PickedQuantity > 0))
        {
            // 貨已在揀貨時離開儲位，這裡只記錄出貨流水，不再動庫存數量。
            var lastPickLocation = await db.PickTasks
                .Where(t => t.OutboundDetailId == detail.Id && t.Status == WmsTaskStatus.COMPLETED)
                .OrderByDescending(t => t.CompletedAt)
                .Select(t => t.SourceLocationId)
                .FirstOrDefaultAsync(ct);

            await engine.LogOnlyAsync(new InventoryMoveContext
            {
                WarehouseId = order.WarehouseId,
                LocationId = lastPickLocation,
                MaterialId = detail.MaterialId,
                Quantity = detail.PickedQuantity,
                TransactionType = TransactionType.SHIP,
                ReferenceType = ReferenceType.OUTBOUND_ORDER,
                ReferenceId = order.Id,
                ReferenceNo = order.OrderNo,
                Remark = request.Remark
            }, ct);

            detail.ShippedQuantity = detail.PickedQuantity;
            detail.Status = OutboundDetailStatus.SHIPPED;
        }

        order.Status = OutboundStatus.SHIPPED;
        order.ShippedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("SHIP", "OUTBOUND", "OutboundOrder", order.Id, order.OrderNo, null, request, ct);

        return await GetAsync(id, ct);
    }

    public async Task<OutboundOrderDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.OutboundOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的出庫單。");

        if (order.Status is OutboundStatus.SHIPPED or OutboundStatus.CANCELLED)
        {
            throw AppException.Conflict($"出庫單狀態為 {order.Status}，無法取消。", "INVALID_STATUS");
        }

        if (order.Details.Any(d => d.PickedQuantity > 0))
        {
            throw AppException.Conflict("已有揀貨紀錄，無法取消；請先處理已揀出的貨品。", "ALREADY_PICKED");
        }

        var detailIds = order.Details.Select(d => d.Id).ToList();
        var openTasks = await db.PickTasks
            .Where(t => detailIds.Contains(t.OutboundDetailId)
                        && t.Status != WmsTaskStatus.COMPLETED
                        && t.Status != WmsTaskStatus.CANCELLED)
            .ToListAsync(ct);

        foreach (var task in openTasks)
        {
            var inventory = await engine.FindAsync(task.SourceLocationId, task.MaterialId, ct: ct);
            if (inventory is not null)
            {
                await engine.ReleaseAsync(inventory, task.Quantity - task.PickedQuantity,
                    ReferenceType.OUTBOUND_ORDER, order.Id, order.OrderNo, ct);
            }
            task.Status = WmsTaskStatus.CANCELLED;
        }

        foreach (var detail in order.Details)
        {
            detail.AllocatedQuantity = 0;
            detail.Status = OutboundDetailStatus.CANCELLED;
        }

        order.Status = OutboundStatus.CANCELLED;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("CANCEL", "OUTBOUND", "OutboundOrder", order.Id, order.OrderNo, null, null, ct);

        return await GetAsync(id, ct);
    }
}
