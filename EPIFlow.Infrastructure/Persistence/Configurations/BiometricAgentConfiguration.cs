using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EPIFlow.Infrastructure.Persistence.Configurations;

public class BiometricAgentConfiguration : IEntityTypeConfiguration<BiometricAgent>
{
    public void Configure(EntityTypeBuilder<BiometricAgent> builder)
    {
        builder.ToTable("BiometricAgents");

        builder.Property(agent => agent.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(agent => agent.MachineName)
            .HasMaxLength(150);

        builder.Property(agent => agent.Description)
            .HasMaxLength(250);

        builder.Property(agent => agent.ApiKey)
            .IsRequired();

        builder.Property(agent => agent.PollingIntervalSeconds)
            .HasDefaultValue(5);

        builder.HasIndex(agent => new { agent.TenantId, agent.Name })
            .IsUnique();

        builder.HasIndex(agent => agent.ApiKey)
            .IsUnique();

        builder.HasMany(agent => agent.Tasks)
            .WithOne(task => task.BiometricAgent!)
            .HasForeignKey(task => task.BiometricAgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
