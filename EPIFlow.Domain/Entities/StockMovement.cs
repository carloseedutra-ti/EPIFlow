using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Domain.Entities;

public class StockMovement : AuditableEntity
{
    [Required]
    public Guid InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public StockMovementType MovementType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
