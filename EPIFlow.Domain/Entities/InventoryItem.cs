using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;

namespace EPIFlow.Domain.Entities;

public class InventoryItem : AuditableEntity
{
    [Required]
    public Guid EpiTypeId { get; set; }

    public EpiType? EpiType { get; set; }

    [Range(0, int.MaxValue)]
    public int QuantityAvailable { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumQuantity { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    public DateTime? LastInventoryDate { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
