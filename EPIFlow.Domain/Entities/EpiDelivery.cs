using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;

namespace EPIFlow.Domain.Entities;

public class EpiDelivery : AuditableEntity
{
    [Required]
    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;

    [Required]
    public Guid ResponsibleUserId { get; set; }

    [MaxLength(50)]
    public string? DeliveryNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<DeliveryItem> Items { get; set; } = new List<DeliveryItem>();
}
