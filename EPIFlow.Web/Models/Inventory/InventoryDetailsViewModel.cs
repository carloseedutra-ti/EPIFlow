using System;
using System.Collections.Generic;

namespace EPIFlow.Web.Models.Inventory;

public class InventoryDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid EpiTypeId { get; set; }
    public string EpiCode { get; set; } = string.Empty;
    public string EpiDescription { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int MinimumQuantity { get; set; }
    public string? Location { get; set; }
    public DateTime? LastInventoryDate { get; set; }
    public IReadOnlyCollection<StockMovementViewModel> RecentMovements { get; set; } = new List<StockMovementViewModel>();
}
