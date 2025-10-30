using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Tenants.DTOs;

public class TenantUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? LegalName { get; set; }

    [MaxLength(20)]
    public string? Document { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(150)]
    public string? AddressComplement { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(80)]
    public string? Country { get; set; }

    [MaxLength(200)]
    public string? ResponsibleName { get; set; }

    [MaxLength(150)]
    [EmailAddress]
    public string? ResponsibleEmail { get; set; }

    [Range(1, 10000)]
    public int EmployeeLimit { get; set; }

    public bool IsActive { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime? SubscriptionExpiresOnUtc { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public string? LogoPath { get; set; }
}
