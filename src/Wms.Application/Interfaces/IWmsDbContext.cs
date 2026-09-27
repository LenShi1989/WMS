using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Wms.Domain.Entities;

namespace Wms.Application.Interfaces;

/// <summary>讓 Application 層存取資料，但不依賴具體 DbContext 實作。</summary>
public interface IWmsDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    DbSet<MaterialCategory> MaterialCategories { get; }
    DbSet<UnitOfMeasure> UnitOfMeasures { get; }
    DbSet<Material> Materials { get; }
    DbSet<MaterialBarcode> MaterialBarcodes { get; }

    DbSet<Warehouse> Warehouses { get; }
    DbSet<WarehouseZone> WarehouseZones { get; }
    DbSet<Location> Locations { get; }

    DbSet<InboundOrder> InboundOrders { get; }
    DbSet<InboundDetail> InboundDetails { get; }
    DbSet<PutawayTask> PutawayTasks { get; }

    DbSet<OutboundOrder> OutboundOrders { get; }
    DbSet<OutboundDetail> OutboundDetails { get; }
    DbSet<PickTask> PickTasks { get; }

    DbSet<Inventory> Inventories { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }

    DbSet<TransferOrder> TransferOrders { get; }

    DbSet<Stocktake> Stocktakes { get; }
    DbSet<StocktakeDetail> StocktakeDetails { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
