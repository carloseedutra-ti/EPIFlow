using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.Property(p => p.Reference)
            .HasMaxLength(100);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasOne(p => p.InventoryItem)
            .WithMany(p => p.Movements)
            .HasForeignKey(p => p.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
