using System;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Models;

namespace EPIFlow.Application.Common.Interfaces;

public interface IPlatformUserService
{
    Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<PlatformUserInfo?> GetTenantAdministratorAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task CreateTenantAdministratorAsync(Guid tenantId, string name, string email, string password, CancellationToken cancellationToken = default);
}
