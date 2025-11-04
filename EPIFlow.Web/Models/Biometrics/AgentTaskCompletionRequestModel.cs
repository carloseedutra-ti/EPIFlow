using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Biometrics;

public class AgentTaskCompletionRequestModel
{
    [Required]
    public Guid ApiKey { get; set; }

    [Required]
    public Guid TaskId { get; set; }

    public string? TemplateBase64 { get; set; }
}
