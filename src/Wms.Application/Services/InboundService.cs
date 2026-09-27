using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

public interface IInboundService
{
    Task<PagedResult<InboundOrderDto>> QueryAsync(InboundQuery query, CancellationToken ct = default);
    Task<InboundOrderDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<InboundOrderDto> CreateAsync(InboundOrderCreateDto dto, CancellationToken ct = default);
    Task<InboundOrderDto> ReceiveAsync(Guid id, ReceiveRequest request, CancellationToken ct = default);
    Task<InboundOrderDto> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<InboundOrderDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public class InboundService(
    IWmsDbContext db,
    IInventoryEngine engine,
    INumberGenerator numberGenerator,
    ICurrentUser currentUser,
    IAuditService audit) : IInboundService
{
    public Task<PagedResult<InboundOrderDto>> QueryAsync(InboundQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.InboundOrders
            .AsNoTracking()
            .WhereIf(query.WarehouseId.HasValue, o => o.WarehouseId == query.WarehouseId)
            .WhereIf(query.Status.HasValue, o => o.Status == query.Status)
            .WhereIf(query.DateFrom.HasValue, o => o.CreatedAt >= query.DateFrom!.Value)
            .WhereIf(query.DateTo.HasValue, o => o.CreatedAt < query.DateTo!.Value.AddDays(1))
            .WhereIf(!string.IsNullOrEmpty(keyword),
                o => o.OrderNo.ToLower().Contains(keyword)
                     || (o.ExternalOrderNo != null && o.ExternalOrderNo.ToLower().Contains(keyword))
                     || (o.SupplierName != null && o.SupplierName.ToLower().Contains(keyword)))
            .OrderByDescending(o => o.CreatedAt);

        return q.ToPagedResultAsync(query, o => new InboundOrderDto
        {
            Id = o.Id,
            OrderNo = o.OrderNo,
            SourceType = o.SourceType,
            ExternalOrderNo = o.ExternalOrderNo,
            SupplierCode = o.SupplierCode,
            SupplierName = o.SupplierName,
            WarehouseId = o.WarehouseId,
            WarehouseCode = o.Warehouse.Code,
            WarehouseName = o.Warehouse.Name,
            Status = o.Status,
            ExpectedArrivalDate = o.ExpectedArrivalDate,
            ReceivedAt = o.ReceivedAt,
            CompletedAt = o.CompletedAt,
            Remark = o.Remark,
            CreatedByName = db.Users.Where(u => u.Id == o.CreatedBy).Select(u => u.DisplayName).FirstOrDefault(),
            CreatedAt = o.CreatedAt,
            LineCount = o.Details.Count,
            TotalOrderedQuantity = o.Details.Sum(d => (decimal?)d.OrderedQuantity) ?? 0,
            TotalReceivedQuantity = o.Details.Sum(d => (decimal?)d.ReceivedQuantity) ?? 0
        }, ct);
    }

    public async Task<InboundOrderDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.InboundOrders
            .AsNoTracking()
            .Include(o => o.Warehouse)
            .Include(o => o.Details).ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的入庫單。");

        var createdByName = await db.Users
            .Where(u => u.Id == order.CreatedBy)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct);

        return new InboundOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            SourceType = order.SourceType,
            ExternalOrderNo = order.ExternalOrderNo,
            SupplierCode = order.SupplierCode,
            SupplierName = order.SupplierName,
            WarehouseId = order.WarehouseId,
            WarehouseCode = order.Warehouse.Code,
            WarehouseName = order.Warehouse.Name,
            Status = order.Status,
            ExpectedArrivalDate = order.ExpectedArrivalDate,
            ReceivedAt = order.ReceivedAt,
            CompletedAt = order.CompletedAt,
            Remark = order.Remark,
            CreatedByName = createdByName,
            CreatedAt = order.CreatedAt,
            LineCount = order.Details.Count,
            TotalOrderedQuantity = order.Details.Sum(d => d.OrderedQuantity),
            TotalReceivedQuantity = order.Details.Sum(d => d.ReceivedQuantity),
            Details = [.. order.Details.OrderBy(d => d.LineNo).Select(d => new InboundDetailDto
            {
                Id = d.Id,
                LineNo = d.LineNo,
                MaterialId = d.MaterialId,
                MaterialCode = d.Material.Code,
                MaterialName = d.Material.Name,
                BaseUom = d.Material.BaseUom,
                OrderedQuantity = d.OrderedQuantity,
                ReceivedQuantity = d.ReceivedQuantity,
                PutawayQuantity = d.PutawayQuantity,
                Status = d.Status,
                Remark = d.Remark
            })]
        };
    }

    public async Task<InboundOrderDto> CreateAsync(InboundOrderCreateDto dto, CancellationToken ct = default)
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
                throw new AppException($"物料 {material.Code} 已停用，無法建立入庫單。", "MATERIAL_INACTIVE");
            }
        }

        var order = new InboundOrder
        {
            OrderNo = await numberGenerator.NextAsync("IB", ct),
            SourceType = dto.SourceType,
            ExternalOrderNo = dto.ExternalOrderNo,
            SupplierCode = dto.SupplierCode,
            SupplierName = dto.SupplierName,
            WarehouseId = dto.WarehouseId,
            Status = InboundStatus.DRAFT,
            ExpectedArrivalDate = dto.ExpectedArrivalDate,
            Remark = dto.Remark,
            CreatedBy = currentUser.UserId
        };

        var lineNo = 1;
        foreach (var line in dto.Details)
        {
            order.Details.Add(new InboundDetail
            {
                LineNo = lineNo++,
                MaterialId = line.MaterialId,
                OrderedQuantity = line.OrderedQuantity,
                Status = InboundDetailStatus.PENDING,
                Remark = line.Remark
            });
        }

        db.InboundOrders.Add(order);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "INBOUND", "InboundOrder", order.Id, order.OrderNo, null, dto, ct);

        return await GetAsync(order.Id, ct);
    }

    public async Task<InboundOrderDto> ReceiveAsync(Guid id, ReceiveRequest request, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.InboundOrders
            .Include(o => o.Details).ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的入庫單。");

        if (order.Status is InboundStatus.COMPLETED or InboundStatus.CANCELLED)
        {
            throw AppException.Conflict($"入庫單狀態為 {order.Status}，無法收貨。", "INVALID_STATUS");
        }

        var receivingLocation = await ResolveReceivingLocationAsync(order.WarehouseId, request.ReceivingLocationId, ct);

        foreach (var line in request.Lines)
        {
            var detail = order.Details.FirstOrDefault(d => d.Id == line.InboundDetailId)
                ?? throw AppException.NotFound($"入庫單中找不到明細 {line.InboundDetailId}。");

            var remaining = detail.OrderedQuantity - detail.ReceivedQuantity;
            if (line.ReceivedQuantity > remaining)
            {
                throw new AppException(
                    $"物料 {detail.Material.Code} 收貨數量超出未收數量：未收 {remaining:0.######}，本次 {line.ReceivedQuantity:0.######}。",
                    "OVER_RECEIVE");
            }

            detail.ReceivedQuantity += line.ReceivedQuantity;
            detail.Status = detail.ReceivedQuantity >= detail.OrderedQuantity
                ? InboundDetailStatus.RECEIVED
                : InboundDetailStatus.PARTIAL;

            // 收貨先讓庫存落在收貨區，後續由上架任務搬到儲存儲位。
            await engine.MoveInAsync(new InventoryMoveContext
            {
                WarehouseId = order.WarehouseId,
                LocationId = receivingLocation.Id,
                MaterialId = detail.MaterialId,
                Quantity = line.ReceivedQuantity,
                TransactionType = TransactionType.RECEIVE,
                ReferenceType = ReferenceType.INBOUND_ORDER,
                ReferenceId = order.Id,
                ReferenceNo = order.OrderNo,
                Remark = request.Remark
            }, ct);

            // 每次收貨都產生對應的上架任務。
            db.PutawayTasks.Add(new PutawayTask
            {
                TaskNo = await numberGenerator.NextAsync("PA", ct),
                InboundDetailId = detail.Id,
                MaterialId = detail.MaterialId,
                WarehouseId = order.WarehouseId,
                Quantity = line.ReceivedQuantity,
                SourceLocationId = receivingLocation.Id,
                TargetLocationId = await SuggestStorageLocationAsync(order.WarehouseId, detail.MaterialId, ct),
                Status = WmsTaskStatus.PENDING,
                Priority = 5
            });
        }

        order.ReceivedAt ??= DateTime.UtcNow;
        order.Status = order.Details.All(d => d.ReceivedQuantity >= d.OrderedQuantity)
            ? InboundStatus.RECEIVED
            : InboundStatus.RECEIVING;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync("RECEIVE", "INBOUND", "InboundOrder", order.Id, order.OrderNo, null, request, ct);

        return await GetAsync(id, ct);
    }

    public async Task<InboundOrderDto> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.InboundOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的入庫單。");

        if (order.Status is InboundStatus.COMPLETED or InboundStatus.CANCELLED)
        {
            throw AppException.Conflict($"入庫單狀態為 {order.Status}，無法結案。", "INVALID_STATUS");
        }

        var openTasks = await db.PutawayTasks
            .CountAsync(t => t.InboundDetail.InboundOrderId == id
                             && t.Status != WmsTaskStatus.COMPLETED
                             && t.Status != WmsTaskStatus.CANCELLED, ct);

        if (openTasks > 0)
        {
            throw AppException.Conflict($"尚有 {openTasks} 筆上架任務未完成，無法結案。", "TASK_PENDING");
        }

        foreach (var detail in order.Details)
        {
            detail.Status = InboundDetailStatus.COMPLETED;
        }

        order.Status = InboundStatus.COMPLETED;
        order.CompletedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("COMPLETE", "INBOUND", "InboundOrder", order.Id, order.OrderNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    public async Task<InboundOrderDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.InboundOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的入庫單。");

        if (order.Status != InboundStatus.DRAFT)
        {
            throw AppException.Conflict("只有草稿狀態的入庫單可以取消。", "INVALID_STATUS");
        }

        order.Status = InboundStatus.CANCELLED;
        order.UpdatedAt = DateTime.UtcNow;
        foreach (var detail in order.Details)
        {
            detail.Status = InboundDetailStatus.CANCELLED;
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CANCEL", "INBOUND", "InboundOrder", order.Id, order.OrderNo, null, null, ct);

        return await GetAsync(id, ct);
    }

    /// <summary>取得收貨暫存儲位；倉庫若尚未建立收貨區，會自動補一個虛擬收貨儲位。</summary>
    private async Task<Location> ResolveReceivingLocationAsync(Guid warehouseId, Guid? requestedId, CancellationToken ct)
    {
        if (requestedId.HasValue)
        {
            var requested = await db.Locations.FirstOrDefaultAsync(l => l.Id == requestedId, ct)
                ?? throw AppException.NotFound("找不到指定的收貨儲位。");

            if (requested.WarehouseId != warehouseId)
            {
                throw new AppException("收貨儲位不屬於此入庫單的倉庫。", "INVALID_LOCATION");
            }

            return requested;
        }

        var receiving = await db.Locations
            .Where(l => l.WarehouseId == warehouseId
                        && l.IsActive
                        && l.Zone != null
                        && l.Zone.ZoneType == ZoneType.RECEIVING)
            .OrderBy(l => l.Code)
            .FirstOrDefaultAsync(ct);

        if (receiving is not null)
        {
            return receiving;
        }

        var warehouse = await db.Warehouses.FirstAsync(w => w.Id == warehouseId, ct);

        var zone = await db.WarehouseZones
            .FirstOrDefaultAsync(z => z.WarehouseId == warehouseId && z.ZoneType == ZoneType.RECEIVING, ct);

        if (zone is null)
        {
            zone = new WarehouseZone
            {
                WarehouseId = warehouseId,
                Code = "RCV",
                Name = "收貨區",
                ZoneType = ZoneType.RECEIVING
            };
            db.WarehouseZones.Add(zone);
        }

        receiving = new Location
        {
            WarehouseId = warehouseId,
            ZoneId = zone.Id,
            Code = $"{warehouse.Code}-RCV-01",
            Name = "收貨暫存區",
            LocationType = LocationType.STAGING
        };
        db.Locations.Add(receiving);

        return receiving;
    }

    /// <summary>建議上架儲位：優先沿用該物料既有的儲存儲位，否則挑一個空儲位。</summary>
    private async Task<Guid?> SuggestStorageLocationAsync(Guid warehouseId, Guid materialId, CancellationToken ct)
    {
        var existing = await db.Inventories
            .Where(i => i.WarehouseId == warehouseId
                        && i.MaterialId == materialId
                        && i.Quantity > 0
                        && i.Location.IsActive
                        && i.Location.Zone != null
                        && i.Location.Zone.ZoneType == ZoneType.STORAGE)
            .OrderByDescending(i => i.Quantity)
            .Select(i => (Guid?)i.LocationId)
            .FirstOrDefaultAsync(ct);

        if (existing.HasValue)
        {
            return existing;
        }

        return await db.Locations
            .Where(l => l.WarehouseId == warehouseId
                        && l.IsActive
                        && l.Zone != null
                        && l.Zone.ZoneType == ZoneType.STORAGE)
            .OrderBy(l => db.Inventories.Where(i => i.LocationId == l.Id).Sum(i => (decimal?)i.Quantity) ?? 0)
            .ThenBy(l => l.Code)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(ct);
    }
}
