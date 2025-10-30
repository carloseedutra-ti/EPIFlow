using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class EpiDeliveryConfiguration : IEntityTypeConfiguration<EpiDelivery>
{
    public void Configure(EntityTypeBuilder<EpiDelivery> builder)
    {
        builder.ToTable("EpiDeliveries");

        builder.Property(p => p.DeliveryNumber)
            .HasMaxLength(50);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasOne(p => p.Employee)
            .WithMany(p => p.Deliveries)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Items)
            .WithOne(p => p.Delivery)
            .HasForeignKey(p => p.EpiDeliveryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
