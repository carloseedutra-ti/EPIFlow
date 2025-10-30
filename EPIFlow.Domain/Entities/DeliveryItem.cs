using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;

namespace EPIFlow.Domain.Entities;

public class DeliveryItem : AuditableEntity
{
    [Required]
    public Guid EpiDeliveryId { get; set; }

    public EpiDelivery? Delivery { get; set; }

    [Required]
    public Guid EpiTypeId { get; set; }

    public EpiType? EpiType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime ValidUntil { get; set; }

    public DateTime? ReturnedAtUtc { get; set; }

    public int? ReturnedQuantity { get; set; }
}
