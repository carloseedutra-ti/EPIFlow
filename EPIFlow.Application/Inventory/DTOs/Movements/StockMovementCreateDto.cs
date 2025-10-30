using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Inventory.DTOs.Movements;

public class StockMovementCreateDto
{
    [Required]
    public Guid InventoryItemId { get; set; }

    [Required]
    public StockMovementType MovementType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
