using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Employees.DTOs;

public class EmployeeCreateDto
{
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

    [Required]
    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow.Date;
}
