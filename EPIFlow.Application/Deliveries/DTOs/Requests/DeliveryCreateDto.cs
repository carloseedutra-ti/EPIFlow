using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Deliveries.DTOs.Requests;

public class DeliveryCreateDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? DeliveryNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<DeliveryItemCreateDto> Items { get; set; } = new();
}
