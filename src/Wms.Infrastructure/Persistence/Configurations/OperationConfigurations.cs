using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class TransferOrderConfiguration : IEntityTypeConfiguration<TransferOrder>
{
    public void Configure(EntityTypeBuilder<TransferOrder> builder)
    {
        builder.ToTable("transfer_orders");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TransferNo).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Reason).HasMaxLength(200);
        builder.Property(t => t.Remark).HasMaxLength(500);

        builder.HasIndex(t => t.TransferNo).IsUnique();
        builder.HasIndex(t => new { t.WarehouseId, t.Status });
        builder.HasIndex(t => t.CreatedAt);

        builder.HasOne(t => t.Warehouse)
            .WithMany()
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Material)
            .WithMany()
            .HasForeignKey(t => t.MaterialId)
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

public class StocktakeConfiguration : IEntityTypeConfiguration<Stocktake>
{
    public void Configure(EntityTypeBuilder<Stocktake> builder)
    {
        builder.ToTable("stocktakes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StocktakeNo).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Remark).HasMaxLength(500);

        builder.HasIndex(s => s.StocktakeNo).IsUnique();
        builder.HasIndex(s => new { s.WarehouseId, s.Status });

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Zone)
            .WithMany()
            .HasForeignKey(s => s.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class StocktakeDetailConfiguration : IEntityTypeConfiguration<StocktakeDetail>
{
    public void Configure(EntityTypeBuilder<StocktakeDetail> builder)
    {
        builder.ToTable("stocktake_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Remark).HasMaxLength(500);

        builder.HasIndex(d => d.StocktakeId);
        builder.HasIndex(d => new { d.StocktakeId, d.LocationId, d.MaterialId }).IsUnique();

        builder.HasOne(d => d.Stocktake)
            .WithMany(s => s.Details)
            .HasForeignKey(d => d.StocktakeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Location)
            .WithMany()
            .HasForeignKey(d => d.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Material)
            .WithMany()
            .HasForeignKey(d => d.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Username).HasMaxLength(50);
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Module).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ReferenceType).HasMaxLength(50);
        builder.Property(a => a.ReferenceNo).HasMaxLength(50);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.OldValue).HasColumnType("jsonb");
        builder.Property(a => a.NewValue).HasColumnType("jsonb");

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.Module, a.Action });
        builder.HasIndex(a => a.UserId);
    }
}
