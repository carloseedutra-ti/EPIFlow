using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Common.Models;
using EPIFlow.Domain.Constants;
using EPIFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Infrastructure.Services;

public class PlatformUserService : IPlatformUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public PlatformUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new ApplicationRole(roleName));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Não foi possível criar a role '{roleName}': {errors}");
        }
    }

    public async Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    public async Task<PlatformUserInfo?> GetTenantAdministratorAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? null
            : new PlatformUserInfo(user.Id, user.Email!, user.FullName, user.IsActive);
    }

    public async Task CreateTenantAdministratorAsync(Guid tenantId, string name, string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            TenantId = tenantId,
            FullName = name.Trim(),
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Não foi possível criar o usuário administrador: {errors}");
        }

        await EnsureRoleExistsAsync(SystemRoles.Administrator, cancellationToken);
        await _userManager.AddToRoleAsync(user, SystemRoles.Administrator);
    }
}
