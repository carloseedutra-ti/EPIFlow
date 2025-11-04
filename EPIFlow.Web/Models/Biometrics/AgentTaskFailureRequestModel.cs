using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Biometrics;

public class AgentTaskFailureRequestModel
{
    [Required]
    public Guid ApiKey { get; set; }

    [Required]
    public Guid TaskId { get; set; }

    [MaxLength(300)]
    public string? Reason { get; set; }
}
