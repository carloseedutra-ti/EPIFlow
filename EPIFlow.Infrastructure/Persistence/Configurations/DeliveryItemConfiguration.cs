using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class DeliveryItemConfiguration : IEntityTypeConfiguration<DeliveryItem>
{
    public void Configure(EntityTypeBuilder<DeliveryItem> builder)
    {
        builder.ToTable("DeliveryItems");

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.HasOne(p => p.EpiType)
            .WithMany(p => p.DeliveryItems)
            .HasForeignKey(p => p.EpiTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
