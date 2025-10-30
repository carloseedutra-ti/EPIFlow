using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Tenants.DTOs;

public record TenantDetailDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? Document,
    string? PhoneNumber,
    string? Address,
    string? AddressComplement,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string? ResponsibleName,
    string? ResponsibleEmail,
    bool IsActive,
    bool IsSuspended,
    int EmployeeLimit,
    DateTime ActiveSinceUtc,
    DateTime? SuspendedAtUtc,
    DateTime? SubscriptionExpiresOnUtc,
    string? Notes,
    int ActiveEmployees,
    int TotalDeliveries,
    int TotalEpiTypes,
    IReadOnlyCollection<TenantPaymentDto> Payments,
    TenantAdminUserDto? Administrator,
    string? LogoPath);
