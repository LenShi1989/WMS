using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("number_sequences");
        builder.HasKey(n => n.Key);
        builder.Property(n => n.Key).HasColumnName("key").HasMaxLength(50);
        builder.Property(n => n.CurrentValue).HasColumnName("current_value");
    }
}
