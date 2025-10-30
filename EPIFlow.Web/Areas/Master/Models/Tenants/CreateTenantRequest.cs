using System.ComponentModel.DataAnnotations;
using EPIFlow.Application.Tenants.DTOs;
using Microsoft.AspNetCore.Http;

namespace EPIFlow.Web.Areas.Master.Models.Tenants;

public class CreateTenantRequest
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

    [Required]
    [MaxLength(200)]
    public string? ResponsibleName { get; set; }

    [Required]
    [MaxLength(150)]
    [EmailAddress]
    public string? ResponsibleEmail { get; set; }

    [Range(1, 10000)]
    public int EmployeeLimit { get; set; } = 10;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(200)]
    public string AdminName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [DataType(DataType.Password)]
    public string AdminPassword { get; set; } = string.Empty;

    public IFormFile? Logo { get; set; }

    public TenantCreateDto ToDto(string? logoPath)
    {
        string? Normalize(string? value, bool required = false, string? fieldName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    throw new ArgumentNullException(fieldName ?? nameof(value), $"O campo '{fieldName ?? "valor"}' é obrigatório.");
                }

                return null;
            }

            return value.Trim();
        }

        var country = string.IsNullOrWhiteSpace(Country) ? "Brasil" : Country.Trim();

        return new TenantCreateDto
        {
            Name = Normalize(Name, true, nameof(Name))!,
            LegalName = Normalize(LegalName),
            Document = Normalize(Document, true, nameof(Document))!,
            PhoneNumber = Normalize(PhoneNumber),
            Address = Normalize(Address, true, nameof(Address))!,
            AddressComplement = Normalize(AddressComplement),
            City = Normalize(City, true, nameof(City))!,
            State = Normalize(State, true, nameof(State))!,
            PostalCode = Normalize(PostalCode, true, nameof(PostalCode))!,
            Country = country,
            ResponsibleName = Normalize(ResponsibleName, true, nameof(ResponsibleName))!,
            ResponsibleEmail = Normalize(ResponsibleEmail, true, nameof(ResponsibleEmail))!,
            EmployeeLimit = EmployeeLimit,
            Notes = Normalize(Notes),
            AdminName = Normalize(AdminName, true, nameof(AdminName))!,
            AdminEmail = Normalize(AdminEmail, true, nameof(AdminEmail))!,
            AdminPassword = AdminPassword,
            LogoPath = logoPath
        };
    }
}
