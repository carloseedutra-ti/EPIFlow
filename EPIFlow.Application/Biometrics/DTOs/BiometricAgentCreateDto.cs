using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Biometrics.DTOs;

public class BiometricAgentCreateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? MachineName { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }

    public int PollingIntervalSeconds { get; set; } = 5;
}
