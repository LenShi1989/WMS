using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class MaterialCategoryConfiguration : IEntityTypeConfiguration<MaterialCategory>
{
    public void Configure(EntityTypeBuilder<MaterialCategory> builder)
    {
        builder.ToTable("material_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Code).IsUnique();
    }
}

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measures");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Code).HasMaxLength(20).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Description).HasMaxLength(500);

        builder.HasIndex(u => u.Code).IsUnique();
    }
}

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Code).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Specification).HasMaxLength(500);
        builder.Property(m => m.BaseUom).HasMaxLength(20).IsRequired();

        builder.HasIndex(m => m.Code).IsUnique();
        builder.HasIndex(m => m.CategoryId);
        builder.HasIndex(m => m.IsActive);

        builder.HasOne(m => m.Category)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class MaterialBarcodeConfiguration : IEntityTypeConfiguration<MaterialBarcode>
{
    public void Configure(EntityTypeBuilder<MaterialBarcode> builder)
    {
        builder.ToTable("material_barcodes");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(b => b.BarcodeType).HasMaxLength(30).IsRequired();

        builder.HasIndex(b => b.Barcode).IsUnique();
        builder.HasIndex(b => b.MaterialId);

        builder.HasOne(b => b.Material)
            .WithMany(m => m.Barcodes)
            .HasForeignKey(b => b.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
