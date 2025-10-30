using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Employees.DTOs;

public class EmployeeUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(14)]
    public string Cpf { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime AdmissionDate { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
}
