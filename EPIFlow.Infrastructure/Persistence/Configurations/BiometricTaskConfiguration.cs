using EPIFlow.Domain.Entities;
using EPIFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class BiometricTaskConfiguration : IEntityTypeConfiguration<BiometricTask>
{
    public void Configure(EntityTypeBuilder<BiometricTask> builder)
    {
        builder.ToTable("BiometricTasks");

        builder.Property(task => task.Finger)
            .HasConversion<int>();

        builder.Property(task => task.Status)
            .HasConversion<int>();

        builder.Property(task => task.EmployeeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(task => task.EmployeeRegistrationNumber)
            .HasMaxLength(50);

        builder.Property(task => task.RequestedByUserName)
            .HasMaxLength(50);

        builder.Property(task => task.CompletedByUserName)
            .HasMaxLength(50);

        builder.Property(task => task.PayloadJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(task => task.ResultJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(task => task.FailureReason)
            .HasMaxLength(300);

        builder.HasIndex(task => new { task.TenantId, task.EmployeeId, task.Finger });

        builder.HasIndex(task => new { task.BiometricAgentId, task.Status, task.CreatedAtUtc });

        builder.HasOne(task => task.Employee)
            .WithMany(employee => employee.BiometricTasks)
            .HasForeignKey(task => task.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
