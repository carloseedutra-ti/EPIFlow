using System;

namespace EPIFlow.Application.Tenants.DTOs;

public record TenantListItemDto(
    Guid Id,
    string Name,
    string? Cnpj,
    string? ResponsibleEmail,
    bool IsActive,
    bool IsSuspended,
    int EmployeeLimit,
    int ActiveEmployees,
    DateTime ActiveSinceUtc,
    string? LogoPath);
