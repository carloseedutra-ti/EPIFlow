using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class TenantPaymentConfiguration : IEntityTypeConfiguration<TenantPayment>
{
    public void Configure(EntityTypeBuilder<TenantPayment> builder)
    {
        builder.ToTable("TenantPayments");

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2);

        builder.Property(payment => payment.Reference)
            .HasMaxLength(100);

        builder.Property(payment => payment.Notes)
            .HasMaxLength(500);

        builder.HasOne(payment => payment.Tenant)
            .WithMany(tenant => tenant.Payments)
            .HasForeignKey(payment => payment.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
