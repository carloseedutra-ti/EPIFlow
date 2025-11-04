using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Deliveries;

public class DeliveryBiometricValidationRequestModel
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid AgentId { get; set; }

    [Required]
    public string Finger { get; set; } = string.Empty;
}
