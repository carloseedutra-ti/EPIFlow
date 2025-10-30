using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Web.Models.EpiTypes;

public class EpiTypeFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o código do EPI.")]
    [Display(Name = "Código")]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a descrição do EPI.")]
    [Display(Name = "Descrição")]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Categoria")]
    public EpiCategory Category { get; set; } = EpiCategory.Other;

    [Display(Name = "Validade (meses)")]
    [Range(0, 120, ErrorMessage = "A validade deve estar entre 0 e 120 meses.")]
    public int ValidityInMonths { get; set; }

    [Display(Name = "CA")]
    [MaxLength(30)]
    public string? CaNumber { get; set; }

    [Display(Name = "Mínimo em estoque")]
    [Range(0, int.MaxValue, ErrorMessage = "Informe um valor válido.")]
    public int MinimumQuantity { get; set; }

    [Display(Name = "Status ativo")]
    public bool IsActive { get; set; } = true;
}
