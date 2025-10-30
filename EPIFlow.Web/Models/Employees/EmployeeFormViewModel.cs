using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Web.Models.Employees;

public class EmployeeFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do colaborador.")]
    [Display(Name = "Nome")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CPF.")]
    [Display(Name = "CPF")]
    [MaxLength(14)]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a matrícula.")]
    [Display(Name = "Matrícula")]
    [MaxLength(30)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Display(Name = "Cargo")]
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [Display(Name = "Setor")]
    [MaxLength(100)]
    public string? Department { get; set; }

    [Display(Name = "Data de admissão")]
    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    [Display(Name = "Situação")]
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
}
