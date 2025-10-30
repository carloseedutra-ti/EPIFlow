using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.EpiTypes.DTOs;

public class EpiTypeCreateDto
{
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public EpiCategory Category { get; set; } = EpiCategory.Other;

    [Range(0, 120)]
    public int ValidityInMonths { get; set; }

    [MaxLength(30)]
    public string? CaNumber { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumQuantity { get; set; } = 0;
}
