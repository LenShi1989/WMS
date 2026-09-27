using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories", t =>
        {
            // 資料庫層的最後一道防線：即使程式有漏洞也不可能出現負庫存或超賣。
            t.HasCheckConstraint("ck_inventories_quantity_non_negative", "quantity >= 0");
            t.HasCheckConstraint("ck_inventories_reserved_non_negative", "reserved_quantity >= 0");
            t.HasCheckConstraint("ck_inventories_reserved_le_quantity", "reserved_quantity <= quantity");
        });

        builder.HasKey(i => i.Id);

        // 一個儲位 + 物料 + 狀態只會有一列庫存。
        builder.HasIndex(i => new { i.LocationId, i.MaterialId, i.Status }).IsUnique();
        builder.HasIndex(i => new { i.WarehouseId, i.MaterialId });
        builder.HasIndex(i => i.MaterialId);

        builder.Property(i => i.AvailableQuantity)
            .HasComputedColumnSql("quantity - reserved_quantity", stored: true);

        builder.HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Location)
            .WithMany()
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Material)
            .WithMany()
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TransactionNo).HasMaxLength(30).IsRequired();
        builder.Property(t => t.ReferenceNo).HasMaxLength(50);
        builder.Property(t => t.Remark).HasMaxLength(500);

        builder.HasIndex(t => t.TransactionNo).IsUnique();
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => new { t.MaterialId, t.CreatedAt });
        builder.HasIndex(t => new { t.ReferenceType, t.ReferenceId });

        builder.HasOne(t => t.Material)
            .WithMany()
            .HasForeignKey(t => t.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Warehouse)
            .WithMany()
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.FromLocation)
            .WithMany()
            .HasForeignKey(t => t.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToLocation)
            .WithMany()
            .HasForeignKey(t => t.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
