using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Users.DTOs;
using EPIFlow.Infrastructure.Identity;
using EPIFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private static readonly StringComparer RoleComparer = StringComparer.OrdinalIgnoreCase;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly EPIFlowDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        EPIFlowDbContext dbContext,
        ITenantProvider tenantProvider)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId)
            .OrderBy(user => user.FullName ?? user.Email)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return Array.Empty<UserListItemDto>();
        }

        var userIds = users.Select(user => user.Id).ToList();
        var defaultAgentIds = users
            .Where(user => user.DefaultBiometricAgentId.HasValue)
            .Select(user => user.DefaultBiometricAgentId!.Value)
            .Distinct()
            .ToList();

        var roleLookup = await BuildRoleLookupAsync(userIds, cancellationToken);
        var agentLookup = await BuildAgentLookupAsync(tenantId, defaultAgentIds, cancellationToken);

        var result = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            var roles = roleLookup.TryGetValue(user.Id, out var roleList)
                ? (IReadOnlyCollection<string>)roleList
                : Array.Empty<string>();

            string? agentName = null;
            if (user.DefaultBiometricAgentId.HasValue &&
                agentLookup.TryGetValue(user.DefaultBiometricAgentId.Value, out var name))
            {
                agentName = name;
            }

            result.Add(new UserListItemDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.Department,
                user.JobTitle,
                user.IsActive,
                roles,
                user.DefaultBiometricAgentId,
                agentName));
        }

        return result;
    }

    public async Task<UserDetailDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Join(_dbContext.Roles, ur => ur.RoleId, role => role.Id, (ur, role) => role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return new UserDetailDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Department,
            user.JobTitle,
            user.DefaultBiometricAgentId,
            user.IsActive,
            roles);
    }

    public async Task<Guid> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var email = dto.Email.Trim();

        await EnsureEmailIsAvailableAsync(email, null, cancellationToken);

        var defaultAgentId = await ValidateDefaultAgentAsync(tenantId, dto.DefaultBiometricAgentId, cancellationToken);

        var user = new ApplicationUser
        {
            TenantId = tenantId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = Normalize(dto.FullName),
            Department = Normalize(dto.Department),
            JobTitle = Normalize(dto.JobTitle),
            DefaultBiometricAgentId = defaultAgentId,
            IsActive = dto.IsActive
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        EnsureSuccess(createResult, "Nao foi possivel criar o usuario");

        await ApplyRolesAsync(user, dto.Roles, cancellationToken);

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var user = await _userManager.Users.FirstOrDefaultAsync(
            u => u.Id == id && u.TenantId == tenantId,
            cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Usuario nao encontrado.");
        }

        var email = dto.Email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureEmailIsAvailableAsync(email, user.Id, cancellationToken);
            user.Email = email;
            user.UserName = email;
        }

        user.FullName = Normalize(dto.FullName);
        user.Department = Normalize(dto.Department);
        user.JobTitle = Normalize(dto.JobTitle);
        user.DefaultBiometricAgentId = await ValidateDefaultAgentAsync(tenantId, dto.DefaultBiometricAgentId, cancellationToken);
        user.IsActive = dto.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSuccess(updateResult, "Nao foi possivel atualizar o usuario");

        await ApplyRolesAsync(user, dto.Roles, cancellationToken);

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            EnsureSuccess(resetResult, "Nao foi possivel alterar a senha do usuario");
        }
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var user = await _userManager.Users.FirstOrDefaultAsync(
            u => u.Id == id && u.TenantId == tenantId,
            cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Usuario nao encontrado.");
        }

        user.IsActive = isActive;
        user.LockoutEnd = isActive ? null : DateTimeOffset.UtcNow.AddYears(10);

        var result = await _userManager.UpdateAsync(user);
        EnsureSuccess(result, "Nao foi possivel ajustar o status do usuario");
    }

    private async Task<IDictionary<Guid, List<string>>> BuildRoleLookupAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, List<string>>();
        }

        var roles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_dbContext.Roles, ur => ur.RoleId, role => role.Id, (ur, role) => new { ur.UserId, role.Name })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .ToListAsync(cancellationToken);

        return roles
            .GroupBy(entry => entry.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(entry => entry.Name!)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    private async Task<IDictionary<Guid, string>> BuildAgentLookupAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> agentIds,
        CancellationToken cancellationToken)
    {
        if (agentIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _dbContext.BiometricAgents
            .AsNoTracking()
            .Where(agent => agent.TenantId == tenantId && agentIds.Contains(agent.Id))
            .ToDictionaryAsync(agent => agent.Id, agent => agent.Name, cancellationToken);
    }

    private Guid GetTenantIdOrThrow()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant atual nao foi identificado.");
        }

        return tenantId.Value;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<Guid?> ValidateDefaultAgentAsync(Guid tenantId, Guid? agentId, CancellationToken cancellationToken)
    {
        if (!agentId.HasValue)
        {
            return null;
        }

        var exists = await _dbContext.BiometricAgents
            .AsNoTracking()
            .AnyAsync(agent => agent.TenantId == tenantId && agent.Id == agentId.Value, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Agente biometrico informado nao pertence ao tenant atual.");
        }

        return agentId;
    }

    private async Task EnsureEmailIsAvailableAsync(string email, Guid? userId, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsNoTracking().Where(user => user.NormalizedEmail == email.ToUpperInvariant());
        if (userId.HasValue)
        {
            query = query.Where(user => user.Id != userId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Ja existe um usuario com este e-mail.");
        }
    }

    private async Task ApplyRolesAsync(ApplicationUser user, IEnumerable<string> roles, CancellationToken cancellationToken)
    {
        var desiredRoles = (roles ?? Array.Empty<string>())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(RoleComparer)
            .ToList();

        await EnsureRolesExistAsync(desiredRoles, cancellationToken);

        var currentRoles = await _userManager.GetRolesAsync(user);

        var toRemove = currentRoles.Where(role => !desiredRoles.Contains(role, RoleComparer)).ToList();
        var toAdd = desiredRoles.Where(role => !currentRoles.Contains(role, RoleComparer)).ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            EnsureSuccess(removeResult, "Nao foi possivel atualizar as permissoes do usuario");
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            EnsureSuccess(addResult, "Nao foi possivel atualizar as permissoes do usuario");
        }
    }

    private async Task EnsureRolesExistAsync(IEnumerable<string> roles, CancellationToken cancellationToken)
    {
        foreach (var role in roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var createResult = await _roleManager.CreateAsync(new ApplicationRole(role));
            EnsureSuccess(createResult, $"Nao foi possivel criar a permissao {role}");
        }
    }

    private static void EnsureSuccess(IdentityResult result, string messagePrefix)
    {
        if (result.Succeeded)
        {
            return;
        }

        var details = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{messagePrefix}: {details}");
    }
}
