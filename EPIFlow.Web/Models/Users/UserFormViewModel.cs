using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Models.Users;

public class UserFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o e-mail do usu\u00E1rio.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail v\u00E1lido.")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "A confirma\u00E7\u00E3o da senha n\u00E3o confere.")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Nome completo")]
    public string? FullName { get; set; }

    [Display(Name = "Departamento")]
    public string? Department { get; set; }

    [Display(Name = "Cargo")]
    public string? JobTitle { get; set; }

    [Display(Name = "Agente padr\u00E3o")]
    public Guid? DefaultBiometricAgentId { get; set; }

    [Display(Name = "Usu\u00E1rio ativo")]
    public bool IsActive { get; set; } = true;

    public List<string> SelectedRoles { get; set; } = new();

    public IEnumerable<SelectListItem> RoleOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> AgentOptions { get; set; } = new List<SelectListItem>();

    public bool RequirePassword { get; set; }
}
