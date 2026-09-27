using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Code).HasMaxLength(50).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(500);

        builder.HasIndex(w => w.Code).IsUnique();
    }
}

public class WarehouseZoneConfiguration : IEntityTypeConfiguration<WarehouseZone>
{
    public void Configure(EntityTypeBuilder<WarehouseZone> builder)
    {
        builder.ToTable("warehouse_zones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Code).HasMaxLength(50).IsRequired();
        builder.Property(z => z.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(z => new { z.WarehouseId, z.Code }).IsUnique();

        builder.HasOne(z => z.Warehouse)
            .WithMany(w => w.Zones)
            .HasForeignKey(z => z.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Code).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(l => l.Code).IsUnique();
        builder.HasIndex(l => new { l.WarehouseId, l.ZoneId });

        builder.HasOne(l => l.Warehouse)
            .WithMany(w => w.Locations)
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Zone)
            .WithMany(z => z.Locations)
            .HasForeignKey(l => l.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
