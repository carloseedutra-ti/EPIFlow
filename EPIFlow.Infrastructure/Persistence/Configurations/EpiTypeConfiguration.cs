using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class EpiTypeConfiguration : IEntityTypeConfiguration<EpiType>
{
    public void Configure(EntityTypeBuilder<EpiType> builder)
    {
        builder.ToTable("EpiTypes");

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.CaNumber)
            .HasMaxLength(30);

        builder.HasIndex(p => new { p.TenantId, p.Code })
            .IsUnique();
    }
}
