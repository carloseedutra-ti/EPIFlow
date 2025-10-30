using System.Collections.Generic;

namespace EPIFlow.Web.Models.Inventory;

public class InventoryIndexViewModel
{
    public string? SearchTerm { get; set; }
    public IReadOnlyCollection<InventoryListItemViewModel> Items { get; set; } = new List<InventoryListItemViewModel>();
}
