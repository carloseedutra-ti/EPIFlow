using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.Property(p => p.QuantityAvailable)
            .HasDefaultValue(0);

        builder.Property(p => p.MinimumQuantity)
            .HasDefaultValue(0);

        builder.Property(p => p.Location)
            .HasMaxLength(150);

        builder.HasIndex(p => new { p.TenantId, p.EpiTypeId })
            .IsUnique();

        builder.HasOne(p => p.EpiType)
            .WithMany(p => p.InventoryItems)
            .HasForeignKey(p => p.EpiTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
