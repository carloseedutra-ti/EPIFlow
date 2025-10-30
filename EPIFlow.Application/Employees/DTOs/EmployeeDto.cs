using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Employees.DTOs;

public record EmployeeDto(
    Guid Id,
    string Name,
    string Cpf,
    string RegistrationNumber,
    string? JobTitle,
    string? Department,
    DateTime AdmissionDate,
    EmployeeStatus Status,
    int TotalDeliveries);
