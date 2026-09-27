using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence;

public class WmsDbContext(DbContextOptions<WmsDbContext> options) : DbContext(options), IWmsDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialBarcode> MaterialBarcodes => Set<MaterialBarcode>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseZone> WarehouseZones => Set<WarehouseZone>();
    public DbSet<Location> Locations => Set<Location>();

    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<InboundDetail> InboundDetails => Set<InboundDetail>();
    public DbSet<PutawayTask> PutawayTasks => Set<PutawayTask>();

    public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
    public DbSet<OutboundDetail> OutboundDetails => Set<OutboundDetail>();
    public DbSet<PickTask> PickTasks => Set<PickTask>();

    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();

    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();
    public DbSet<StocktakeDetail> StocktakeDetails => Set<StocktakeDetail>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);

        ApplyGlobalConventions(modelBuilder);
        ApplySnakeCaseNaming(modelBuilder);

        // xmin 是 PostgreSQL 的系統欄位，必須在命名轉換之後再固定回來。
        modelBuilder.Entity<Inventory>()
            .Property(i => i.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }

    /// <summary>列舉一律存字串、金額數量統一 numeric(18,6)、時間統一 timestamptz。</summary>
    private static void ApplyGlobalConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                if (clrType.IsEnum)
                {
                    property.SetProviderClrType(typeof(string));
                    property.SetMaxLength(40);
                }
                else if (clrType == typeof(decimal))
                {
                    property.SetColumnType("numeric(18,6)");
                }
                else if (clrType == typeof(DateTime))
                {
                    property.SetColumnType("timestamptz");
                }
            }
        }
    }

    /// <summary>把 Table / Column / Index / 外鍵名稱統一轉成 snake_case。</summary>
    private static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is not null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entityType.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (char.IsUpper(current))
            {
                var needsUnderscore = i > 0
                    && name[i - 1] != '_'
                    && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));

                if (needsUnderscore)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
