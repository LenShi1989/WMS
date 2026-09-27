using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Application.Services;

/// <summary>
/// 庫存異動引擎。系統中唯一允許改動 Inventory 數量的地方，
/// 每次改動都會同時產生一筆 InventoryTransaction。
/// </summary>
public interface IInventoryEngine
{
    /// <summary>入庫：指定儲位增加數量。</summary>
    Task<InventoryTransaction> MoveInAsync(InventoryMoveContext ctx, CancellationToken ct = default);

    /// <summary>出庫：指定儲位扣減數量，可同時消耗預留量。</summary>
    Task<InventoryTransaction> MoveOutAsync(InventoryMoveContext ctx, bool consumeReservation, CancellationToken ct = default);

    /// <summary>移庫：來源儲位扣減、目的儲位增加，只產生一筆 TRANSFER。</summary>
    Task<InventoryTransaction> TransferAsync(InventoryTransferContext ctx, CancellationToken ct = default);

    /// <summary>預留庫存，供出庫單分配使用。</summary>
    Task<InventoryTransaction> ReserveAsync(Inventory inventory, decimal quantity, ReferenceType refType, Guid refId, string? refNo, CancellationToken ct = default);

    /// <summary>釋放預留。</summary>
    Task<InventoryTransaction> ReleaseAsync(Inventory inventory, decimal quantity, ReferenceType refType, Guid refId, string? refNo, CancellationToken ct = default);

    /// <summary>依可用量由多到少列出可分配的庫存列。</summary>
    Task<List<Inventory>> FindAllocatableAsync(Guid warehouseId, Guid materialId, CancellationToken ct = default);

    /// <summary>取得指定儲位的庫存列，不存在則回傳 null。</summary>
    Task<Inventory?> FindAsync(Guid locationId, Guid materialId, InventoryStatus status = InventoryStatus.NORMAL, CancellationToken ct = default);

    /// <summary>僅記錄一筆異動流水，不改動庫存數量（例如出貨憑證）。</summary>
    Task<InventoryTransaction> LogOnlyAsync(InventoryMoveContext ctx, CancellationToken ct = default);
}

/// <summary>單一儲位進出的參數。</summary>
public class InventoryMoveContext
{
    public required Guid WarehouseId { get; init; }
    public required Guid LocationId { get; init; }
    public required Guid MaterialId { get; init; }
    public required decimal Quantity { get; init; }
    public required TransactionType TransactionType { get; init; }
    public InventoryStatus Status { get; init; } = InventoryStatus.NORMAL;
    public ReferenceType? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Remark { get; init; }
}

/// <summary>移庫參數。</summary>
public class InventoryTransferContext
{
    public required Guid WarehouseId { get; init; }
    public required Guid FromLocationId { get; init; }
    public required Guid ToLocationId { get; init; }
    public required Guid MaterialId { get; init; }
    public required decimal Quantity { get; init; }
    public InventoryStatus Status { get; init; } = InventoryStatus.NORMAL;

    /// <summary>移庫類的異動也可能是上架（PUTAWAY），預設為一般移庫。</summary>
    public TransactionType TransactionType { get; init; } = TransactionType.TRANSFER;

    public ReferenceType? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Remark { get; init; }
}

public class InventoryEngine(
    IWmsDbContext db,
    INumberGenerator numberGenerator,
    ICurrentUser currentUser) : IInventoryEngine
{
    public async Task<InventoryTransaction> MoveInAsync(InventoryMoveContext ctx, CancellationToken ct = default)
    {
        EnsurePositive(ctx.Quantity);

        var inventory = await GetOrCreateAsync(ctx.WarehouseId, ctx.LocationId, ctx.MaterialId, ctx.Status, ct);
        inventory.Quantity += ctx.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        return await AddTransactionAsync(
            ctx.TransactionType, ctx.MaterialId, ctx.WarehouseId,
            fromLocationId: null, toLocationId: ctx.LocationId,
            ctx.Quantity, inventory.Quantity,
            ctx.ReferenceType, ctx.ReferenceId, ctx.ReferenceNo, ctx.Remark, ct);
    }

    public async Task<InventoryTransaction> MoveOutAsync(InventoryMoveContext ctx, bool consumeReservation, CancellationToken ct = default)
    {
        EnsurePositive(ctx.Quantity);

        var inventory = await FindAsync(ctx.LocationId, ctx.MaterialId, ctx.Status, ct)
            ?? throw AppException.InsufficientStock($"儲位上沒有此物料的庫存，無法扣帳。");

        // 消耗預留時看的是預留量，否則看的是可用量（未被預留的部分）。
        if (consumeReservation)
        {
            if (inventory.ReservedQuantity < ctx.Quantity)
            {
                throw AppException.InsufficientStock(
                    $"預留數量不足：預留 {Fmt(inventory.ReservedQuantity)}，需求 {Fmt(ctx.Quantity)}");
            }
            inventory.ReservedQuantity -= ctx.Quantity;
        }
        else
        {
            var available = inventory.Quantity - inventory.ReservedQuantity;
            if (available < ctx.Quantity)
            {
                throw AppException.InsufficientStock(
                    $"可用庫存不足：可用 {Fmt(available)}，需求 {Fmt(ctx.Quantity)}");
            }
        }

        if (inventory.Quantity < ctx.Quantity)
        {
            throw AppException.InsufficientStock(
                $"庫存不足：現有 {Fmt(inventory.Quantity)}，需求 {Fmt(ctx.Quantity)}");
        }

        inventory.Quantity -= ctx.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        return await AddTransactionAsync(
            ctx.TransactionType, ctx.MaterialId, ctx.WarehouseId,
            fromLocationId: ctx.LocationId, toLocationId: null,
            ctx.Quantity, inventory.Quantity,
            ctx.ReferenceType, ctx.ReferenceId, ctx.ReferenceNo, ctx.Remark, ct);
    }

    public async Task<InventoryTransaction> TransferAsync(InventoryTransferContext ctx, CancellationToken ct = default)
    {
        EnsurePositive(ctx.Quantity);

        if (ctx.FromLocationId == ctx.ToLocationId)
        {
            throw new AppException("來源儲位與目的儲位不可相同。", "INVALID_LOCATION");
        }

        var from = await FindAsync(ctx.FromLocationId, ctx.MaterialId, ctx.Status, ct)
            ?? throw AppException.InsufficientStock("來源儲位沒有此物料的庫存。");

        var available = from.Quantity - from.ReservedQuantity;
        if (available < ctx.Quantity)
        {
            throw AppException.InsufficientStock(
                $"來源儲位可用庫存不足：可用 {Fmt(available)}，需求 {Fmt(ctx.Quantity)}");
        }

        from.Quantity -= ctx.Quantity;
        from.UpdatedAt = DateTime.UtcNow;

        var to = await GetOrCreateAsync(ctx.WarehouseId, ctx.ToLocationId, ctx.MaterialId, ctx.Status, ct);
        to.Quantity += ctx.Quantity;
        to.UpdatedAt = DateTime.UtcNow;

        return await AddTransactionAsync(
            ctx.TransactionType, ctx.MaterialId, ctx.WarehouseId,
            ctx.FromLocationId, ctx.ToLocationId,
            ctx.Quantity, to.Quantity,
            ctx.ReferenceType, ctx.ReferenceId, ctx.ReferenceNo, ctx.Remark, ct);
    }

    public async Task<InventoryTransaction> ReserveAsync(
        Inventory inventory, decimal quantity, ReferenceType refType, Guid refId, string? refNo, CancellationToken ct = default)
    {
        EnsurePositive(quantity);

        var available = inventory.Quantity - inventory.ReservedQuantity;
        if (available < quantity)
        {
            throw AppException.InsufficientStock(
                $"可用庫存不足，無法預留：可用 {Fmt(available)}，需求 {Fmt(quantity)}");
        }

        inventory.ReservedQuantity += quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        return await AddTransactionAsync(
            TransactionType.RESERVE, inventory.MaterialId, inventory.WarehouseId,
            fromLocationId: inventory.LocationId, toLocationId: null,
            quantity, inventory.Quantity, refType, refId, refNo, null, ct);
    }

    public async Task<InventoryTransaction> ReleaseAsync(
        Inventory inventory, decimal quantity, ReferenceType refType, Guid refId, string? refNo, CancellationToken ct = default)
    {
        EnsurePositive(quantity);

        var release = Math.Min(quantity, inventory.ReservedQuantity);
        inventory.ReservedQuantity -= release;
        inventory.UpdatedAt = DateTime.UtcNow;

        return await AddTransactionAsync(
            TransactionType.RELEASE, inventory.MaterialId, inventory.WarehouseId,
            fromLocationId: inventory.LocationId, toLocationId: null,
            release, inventory.Quantity, refType, refId, refNo, null, ct);
    }

    public async Task<List<Inventory>> FindAllocatableAsync(Guid warehouseId, Guid materialId, CancellationToken ct = default)
    {
        // 依可用量大的儲位優先分配，可減少揀貨任務筆數。
        return await db.Inventories
            .Where(i => i.WarehouseId == warehouseId
                        && i.MaterialId == materialId
                        && i.Status == InventoryStatus.NORMAL
                        && i.Quantity > i.ReservedQuantity)
            .OrderByDescending(i => i.Quantity - i.ReservedQuantity)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<Inventory?> FindAsync(
        Guid locationId, Guid materialId, InventoryStatus status = InventoryStatus.NORMAL, CancellationToken ct = default)
    {
        return db.Inventories
            .FirstOrDefaultAsync(i => i.LocationId == locationId
                                      && i.MaterialId == materialId
                                      && i.Status == status, ct);
    }

    public Task<InventoryTransaction> LogOnlyAsync(InventoryMoveContext ctx, CancellationToken ct = default)
    {
        return AddTransactionAsync(
            ctx.TransactionType, ctx.MaterialId, ctx.WarehouseId,
            fromLocationId: ctx.LocationId, toLocationId: null,
            ctx.Quantity, balanceAfter: null,
            ctx.ReferenceType, ctx.ReferenceId, ctx.ReferenceNo, ctx.Remark, ct);
    }

    private async Task<Inventory> GetOrCreateAsync(
        Guid warehouseId, Guid locationId, Guid materialId, InventoryStatus status, CancellationToken ct)
    {
        var inventory = await FindAsync(locationId, materialId, status, ct);
        if (inventory is not null)
        {
            return inventory;
        }

        // 同一次 SaveChanges 內可能已經新增過同一儲位的庫存列，先在追蹤器中找。
        var local = db.Inventories.Local.FirstOrDefault(
            i => i.LocationId == locationId && i.MaterialId == materialId && i.Status == status);
        if (local is not null)
        {
            return local;
        }

        inventory = new Inventory
        {
            WarehouseId = warehouseId,
            LocationId = locationId,
            MaterialId = materialId,
            Status = status,
            Quantity = 0,
            ReservedQuantity = 0
        };
        db.Inventories.Add(inventory);
        return inventory;
    }

    private async Task<InventoryTransaction> AddTransactionAsync(
        TransactionType type, Guid materialId, Guid warehouseId,
        Guid? fromLocationId, Guid? toLocationId,
        decimal quantity, decimal? balanceAfter,
        ReferenceType? refType, Guid? refId, string? refNo, string? remark,
        CancellationToken ct)
    {
        var transaction = new InventoryTransaction
        {
            TransactionNo = await numberGenerator.NextAsync("TX", ct),
            TransactionType = type,
            MaterialId = materialId,
            WarehouseId = warehouseId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            Quantity = quantity,
            BalanceAfter = balanceAfter,
            ReferenceType = refType,
            ReferenceId = refId,
            ReferenceNo = refNo,
            OperatorId = currentUser.UserId,
            Remark = remark
        };

        db.InventoryTransactions.Add(transaction);
        return transaction;
    }

    private static void EnsurePositive(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new AppException("數量必須大於 0。", "INVALID_QUANTITY");
        }
    }

    private static string Fmt(decimal value) => value.ToString("0.######");
}
