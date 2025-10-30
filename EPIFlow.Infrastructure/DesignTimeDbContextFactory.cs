using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Infrastructure.Persistence;
using EPIFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EPIFlow.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EPIFlowDbContext>
{
    public EPIFlowDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ICurrentUserService, DesignTimeCurrentUserService>();
        services.AddSingleton<ITenantProvider, DesignTimeTenantProvider>();

        var optionsBuilder = new DbContextOptionsBuilder<EPIFlowDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("EPIFlow"));

        return new EPIFlowDbContext(optionsBuilder.Options, services.BuildServiceProvider().GetRequiredService<ITenantProvider>(), services.BuildServiceProvider().GetRequiredService<ICurrentUserService>(), services.BuildServiceProvider().GetRequiredService<IDateTimeProvider>());
    }

    private class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Empty;
        public string? GetUserName() => "design-time";
        public bool IsInRole(string role) => true;
    }

    private class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid? GetTenantId() => Guid.Empty;
        public string? GetTenantIdentifier() => "master";
    }
}
