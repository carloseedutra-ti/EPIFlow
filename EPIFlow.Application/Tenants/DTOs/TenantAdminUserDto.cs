using System;

namespace EPIFlow.Application.Tenants.DTOs;

public record TenantAdminUserDto(
    Guid Id,
    string Email,
    string? FullName,
    bool IsActive);
