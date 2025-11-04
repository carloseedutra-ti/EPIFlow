using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Biometrics;

public class BiometricAgentFormViewModel
{
    [Required]
    [MaxLength(150)]
    [Display(Name = "Nome do agente")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    [Display(Name = "Nome da máquina (opcional)")]
    public string? MachineName { get; set; }

    [MaxLength(250)]
    [Display(Name = "Descrição (opcional)")]
    public string? Description { get; set; }

    [Display(Name = "Intervalo de polling (segundos)")]
    [Range(3, 300, ErrorMessage = "Valor deve estar entre 3 e 300 segundos.")]
    public int PollingIntervalSeconds { get; set; } = 5;
}
