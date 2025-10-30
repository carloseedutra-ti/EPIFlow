using System;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Deliveries.Services;
using EPIFlow.Application.Dashboard.Services;
using EPIFlow.Application.Inventory.Services;
using EPIFlow.Web.Models.Dashboard;
using EPIFlow.Web.Models.Deliveries;
using EPIFlow.Web.Models.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IInventoryService _inventoryService;
    private readonly IDeliveryService _deliveryService;

    public DashboardController(
        IDashboardService dashboardService,
        IInventoryService inventoryService,
        IDeliveryService deliveryService)
    {
        _dashboardService = dashboardService;
        _inventoryService = inventoryService;
        _deliveryService = deliveryService;
    }

    public async Task<IActionResult> Index()
    {
        var summary = await _dashboardService.GetSummaryAsync();
        var inventoryItems = await _inventoryService.GetAllAsync(null);
        var lowStockItems = inventoryItems
            .Where(item => item.QuantityAvailable <= item.MinimumQuantity)
            .Select(item => new InventoryListItemViewModel(
                item.Id,
                item.EpiTypeId,
                item.EpiCode,
                item.EpiDescription,
                item.QuantityAvailable,
                item.MinimumQuantity,
                item.Location,
                item.LastInventoryDate,
                item.QuantityAvailable <= item.MinimumQuantity))
            .ToList();

        var deliveries = (await _deliveryService.GetAllAsync(null, null))
            .OrderByDescending(delivery => delivery.DeliveryDate)
            .Take(5)
            .Select(delivery => new DeliveryListItemViewModel(
                delivery.Id,
                delivery.EmployeeId,
                delivery.EmployeeName,
                delivery.DeliveryDate,
                delivery.ResponsibleName,
                delivery.DeliveryNumber,
                delivery.Notes,
                delivery.Items.Select(item => new DeliveryItemViewModel(
                    item.Id,
                    item.EpiCode,
                    item.EpiDescription,
                    item.Quantity,
                    item.ValidUntil,
                    item.ReturnedQuantity ?? 0,
                    item.ReturnedAtUtc)).ToList()))
            .ToList();

        var viewModel = new DashboardViewModel
        {
            TotalEpiTypes = summary.TotalEpiTypes,
            TotalActiveEmployees = summary.TotalActiveEmployees,
            DeliveriesInMonth = summary.DeliveriesInCurrentMonth,
            LowStockItemsCount = lowStockItems.Count,
            LowStockItems = lowStockItems,
            RecentDeliveries = deliveries
        };

        return View(viewModel);
    }
}
