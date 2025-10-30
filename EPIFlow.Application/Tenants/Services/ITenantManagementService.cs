using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Tenants.DTOs;

namespace EPIFlow.Application.Tenants.Services;

public interface ITenantManagementService
{
    Task<TenantDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantListItemDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TenantCreateDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantUpdateDto dto, CancellationToken cancellationToken = default);
    Task SetSuspendedAsync(Guid id, bool isSuspended, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(TenantPaymentCreateDto dto, CancellationToken cancellationToken = default);
}
