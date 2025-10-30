using System.Collections.Generic;
using EPIFlow.Web.Models.Deliveries;
using EPIFlow.Web.Models.Inventory;

namespace EPIFlow.Web.Models.Dashboard;

public class DashboardViewModel
{
    public int TotalEpiTypes { get; set; }
    public int TotalActiveEmployees { get; set; }
    public int DeliveriesInMonth { get; set; }
    public int LowStockItemsCount { get; set; }
    public IReadOnlyCollection<InventoryListItemViewModel> LowStockItems { get; set; } = new List<InventoryListItemViewModel>();
    public IReadOnlyCollection<DeliveryListItemViewModel> RecentDeliveries { get; set; } = new List<DeliveryListItemViewModel>();
}
