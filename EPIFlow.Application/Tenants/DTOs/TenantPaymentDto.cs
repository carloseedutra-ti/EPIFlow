using System;

namespace EPIFlow.Application.Tenants.DTOs;

public record TenantPaymentDto(
    Guid Id,
    decimal Amount,
    DateTime PaymentDateUtc,
    string? Reference,
    string? Notes,
    DateTime CreatedAtUtc);
