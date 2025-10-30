using System;

namespace EPIFlow.Application.Tenants.DTOs;

public record TenantDashboardDto(
    int TotalTenants,
    int ActiveTenants,
    int SuspendedTenants,
    int BlockedTenants,
    decimal TotalPaymentsLast30Days,
    decimal TotalPaymentsOverall);
