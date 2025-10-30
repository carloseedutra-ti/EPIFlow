using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.LegalName)
            .HasMaxLength(250);

        builder.Property(p => p.Document)
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(150);

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(p => p.ResponsibleName)
            .HasMaxLength(200);

        builder.Property(p => p.ResponsibleEmail)
            .HasMaxLength(150);

        builder.Property(p => p.Subdomain)
            .HasMaxLength(100);

        builder.HasIndex(p => p.Subdomain)
            .IsUnique()
            .HasFilter("[Subdomain] IS NOT NULL");

        builder.Property(p => p.Address)
            .HasMaxLength(250);

        builder.Property(p => p.AddressComplement)
            .HasMaxLength(150);

        builder.Property(p => p.City)
            .HasMaxLength(120);

        builder.Property(p => p.State)
            .HasMaxLength(80);

        builder.Property(p => p.PostalCode)
            .HasMaxLength(20);

        builder.Property(p => p.Country)
            .HasMaxLength(80);

        builder.Property(p => p.EmployeeLimit)
            .HasDefaultValue(10);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.LogoPath)
            .HasMaxLength(300);

        builder.Property(p => p.SubscriptionExpiresOnUtc)
            .HasColumnType("datetime2");

        builder.Property(p => p.SuspendedAtUtc)
            .HasColumnType("datetime2");

        builder.HasMany(p => p.Employees)
            .WithOne()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
