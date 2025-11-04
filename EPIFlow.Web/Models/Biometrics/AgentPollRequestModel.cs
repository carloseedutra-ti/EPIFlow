using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Biometrics;

public class AgentPollRequestModel
{
    [Required]
    public Guid ApiKey { get; set; }
}
