using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Deliveries.DTOs.Requests;

public class DeliveryReturnDto
{
    [Required]
    public Guid DeliveryItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int ReturnedQuantity { get; set; }

    [Required]
    public DateTime ReturnedAtUtc { get; set; } = DateTime.UtcNow;
}
