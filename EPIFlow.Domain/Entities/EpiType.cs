using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Domain.Entities;

public class EpiType : AuditableEntity
{
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    public EpiCategory Category { get; set; } = EpiCategory.Other;

    [Range(0, 120)]
    public int ValidityInMonths { get; set; }

    [MaxLength(30)]
    public string? CaNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();
}
