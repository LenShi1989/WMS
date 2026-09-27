using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class InboundOrderConfiguration : IEntityTypeConfiguration<InboundOrder>
{
    public void Configure(EntityTypeBuilder<InboundOrder> builder)
    {
        builder.ToTable("inbound_orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ExternalOrderNo).HasMaxLength(100);
        builder.Property(o => o.SupplierCode).HasMaxLength(50);
        builder.Property(o => o.SupplierName).HasMaxLength(200);
        builder.Property(o => o.Remark).HasMaxLength(500);

        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => new { o.WarehouseId, o.Status });
        builder.HasIndex(o => o.CreatedAt);

        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InboundDetailConfiguration : IEntityTypeConfiguration<InboundDetail>
{
    public void Configure(EntityTypeBuilder<InboundDetail> builder)
    {
        builder.ToTable("inbound_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Remark).HasMaxLength(500);

        builder.HasIndex(d => d.InboundOrderId);

        builder.HasOne(d => d.InboundOrder)
            .WithMany(o => o.Details)
            .HasForeignKey(d => d.InboundOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Material)
            .WithMany()
            .HasForeignKey(d => d.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PutawayTaskConfiguration : IEntityTypeConfiguration<PutawayTask>
{
    public void Configure(EntityTypeBuilder<PutawayTask> builder)
    {
        builder.ToTable("putaway_tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TaskNo).HasMaxLength(30).IsRequired();

        builder.HasIndex(t => t.TaskNo).IsUnique();
        builder.HasIndex(t => new { t.WarehouseId, t.Status });
        builder.HasIndex(t => t.InboundDetailId);

        builder.HasOne(t => t.InboundDetail)
            .WithMany(d => d.PutawayTasks)
            .HasForeignKey(t => t.InboundDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Material)
            .WithMany()
            .HasForeignKey(t => t.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SourceLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.TargetLocation)
            .WithMany()
            .HasForeignKey(t => t.TargetLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OutboundOrderConfiguration : IEntityTypeConfiguration<OutboundOrder>
{
    public void Configure(EntityTypeBuilder<OutboundOrder> builder)
    {
        builder.ToTable("outbound_orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ExternalOrderNo).HasMaxLength(100);
        builder.Property(o => o.CustomerCode).HasMaxLength(50);
        builder.Property(o => o.CustomerName).HasMaxLength(200);
        builder.Property(o => o.Remark).HasMaxLength(500);

        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => new { o.WarehouseId, o.Status });
        builder.HasIndex(o => o.CreatedAt);

        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OutboundDetailConfiguration : IEntityTypeConfiguration<OutboundDetail>
{
    public void Configure(EntityTypeBuilder<OutboundDetail> builder)
    {
        builder.ToTable("outbound_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Remark).HasMaxLength(500);

        builder.HasIndex(d => d.OutboundOrderId);

        builder.HasOne(d => d.OutboundOrder)
            .WithMany(o => o.Details)
            .HasForeignKey(d => d.OutboundOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Material)
            .WithMany()
            .HasForeignKey(d => d.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PickTaskConfiguration : IEntityTypeConfiguration<PickTask>
{
    public void Configure(EntityTypeBuilder<PickTask> builder)
    {
        builder.ToTable("pick_tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TaskNo).HasMaxLength(30).IsRequired();

        builder.HasIndex(t => t.TaskNo).IsUnique();
        builder.HasIndex(t => new { t.WarehouseId, t.Status });
        builder.HasIndex(t => t.OutboundDetailId);

        builder.HasOne(t => t.OutboundDetail)
            .WithMany(d => d.PickTasks)
            .HasForeignKey(t => t.OutboundDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Material)
            .WithMany()
            .HasForeignKey(t => t.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SourceLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
