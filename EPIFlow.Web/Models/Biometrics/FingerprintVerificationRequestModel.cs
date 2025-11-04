using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Biometrics;

public class FingerprintVerificationRequestModel
{
    [Required]
    public Guid AgentId { get; set; }

    [Required]
    public string Finger { get; set; } = string.Empty;
}
