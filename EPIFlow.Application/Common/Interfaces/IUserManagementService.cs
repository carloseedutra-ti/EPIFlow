using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Users.DTOs;

namespace EPIFlow.Application.Common.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
