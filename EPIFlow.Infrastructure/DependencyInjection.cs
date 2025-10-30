using System;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Infrastructure.Identity;
using EPIFlow.Infrastructure.Persistence;
using EPIFlow.Infrastructure.Repositories;
using EPIFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EPIFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? configuration.GetConnectionString("EPIFlow")
                               ?? configuration["ConnectionStrings:EPIFlow"]
                               ?? throw new InvalidOperationException("Database connection string was not provided.");

        services.AddDbContext<EPIFlowDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(EPIFlowDbContext).Assembly.FullName);
            });

#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<EPIFlowDbContext>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        services.AddHttpContextAccessor();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IPlatformUserService, PlatformUserService>();

        return services;
    }
}
