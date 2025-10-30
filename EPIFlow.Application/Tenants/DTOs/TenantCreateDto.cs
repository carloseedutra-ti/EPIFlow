using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Tenants.DTOs;

public class TenantCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? LegalName { get; set; }

    [Required]
    [MaxLength(20)]
    public string? Document { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(200)]
    public string? ResponsibleName { get; set; }

    [Required]
    [MaxLength(150)]
    [EmailAddress]
    public string? ResponsibleEmail { get; set; }

    [Required]
    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(150)]
    public string? AddressComplement { get; set; }

    [Required]
    [MaxLength(120)]
    public string? City { get; set; }

    [Required]
    [MaxLength(80)]
    public string? State { get; set; }

    [Required]
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [MaxLength(80)]
    public string? Country { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Range(1, 10000)]
    public int EmployeeLimit { get; set; } = 10;

    [Required]
    [MaxLength(200)]
    public string AdminName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string AdminPassword { get; set; } = string.Empty;

    public string? LogoPath { get; set; }
}
