using System;
using System.Threading.Tasks;
using EPIFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EPIFlow.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    private const string MasterEmail = "master@epiflow.com";
    private const string MasterPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            var dbContext = scopedProvider.GetRequiredService<EPIFlowDbContext>();
            await dbContext.Database.MigrateAsync();

            await EnsureMasterUserAsync(scopedProvider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task EnsureMasterUserAsync(IServiceProvider scopedProvider)
    {
        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scopedProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await EnsureRolesAsync(roleManager);

        var masterUser = await userManager.FindByEmailAsync(MasterEmail);
        if (masterUser != null)
        {
            return;
        }

        masterUser = new ApplicationUser
        {
            UserName = MasterEmail,
            Email = MasterEmail,
            EmailConfirmed = true,
            FullName = "Administrador Master",
            TenantId = Guid.Empty,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(masterUser, MasterPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors);
            throw new InvalidOperationException($"Failed to create master user: {errors}");
        }

        await userManager.AddToRoleAsync(masterUser, Domain.Constants.SystemRoles.Administrator);
    }

    private static async Task EnsureRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            Domain.Constants.SystemRoles.Administrator,
            Domain.Constants.SystemRoles.Warehouse,
            Domain.Constants.SystemRoles.User
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole(role));
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors);
                    throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
                }
            }
        }
    }
}
