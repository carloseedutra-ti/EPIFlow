using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Deliveries.DTOs.Requests;

public class DeliveryItemCreateDto
{
    [Required]
    public Guid EpiTypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public DateTime ValidUntil { get; set; }
}
