using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Web.Models.Inventory;

public class StockMovementFormViewModel
{
    [Required]
    public Guid InventoryItemId { get; set; }

    [Display(Name = "Tipo de movimentação")]
    [Required]
    public StockMovementType MovementType { get; set; }

    [Display(Name = "Quantidade")]
    [Range(1, int.MaxValue, ErrorMessage = "Informe uma quantidade válida.")]
    public int Quantity { get; set; }

    [Display(Name = "Referência")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Display(Name = "Observações")]
    [MaxLength(500)]
    public string? Notes { get; set; }
}
