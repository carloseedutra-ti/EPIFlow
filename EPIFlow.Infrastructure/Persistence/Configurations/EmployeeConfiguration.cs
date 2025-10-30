using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Cpf)
            .IsRequired()
            .HasMaxLength(14);

        builder.Property(p => p.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.JobTitle)
            .HasMaxLength(100);

        builder.Property(p => p.Department)
            .HasMaxLength(100);

        builder.HasIndex(p => new { p.TenantId, p.Cpf })
            .IsUnique();

        builder.HasIndex(p => new { p.TenantId, p.RegistrationNumber })
            .IsUnique();
    }
}
