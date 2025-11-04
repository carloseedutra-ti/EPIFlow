using System;
using System.Collections.Generic;
using EPIFlow.Domain.Common;

namespace EPIFlow.Domain.Entities;

public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Document { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ResponsibleName { get; set; }
    public string? ResponsibleEmail { get; set; }
    public string? Address { get; set; }
    public string? AddressComplement { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Subdomain { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ActiveSinceUtc { get; set; } = DateTime.UtcNow;
    public bool IsSuspended { get; set; }
    public DateTime? SuspendedAtUtc { get; set; }
    public int EmployeeLimit { get; set; } = 10;
    public DateTime? SubscriptionExpiresOnUtc { get; set; }
    public string? Notes { get; set; }
    public string? LogoPath { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<EpiType> EpiTypes { get; set; } = new List<EpiType>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<EpiDelivery> Deliveries { get; set; } = new List<EpiDelivery>();
    public ICollection<TenantPayment> Payments { get; set; } = new List<TenantPayment>();
    public ICollection<BiometricAgent> BiometricAgents { get; set; } = new List<BiometricAgent>();
}
