using System;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Inventory.DTOs.Movements;
using EPIFlow.Application.Inventory.Services;
using EPIFlow.Domain.Enums;
using EPIFlow.Domain.Constants;
using EPIFlow.Web.Models.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator + "," + SystemRoles.Warehouse)]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var items = await _inventoryService.GetAllAsync(search);
        var viewModel = new InventoryIndexViewModel
        {
            SearchTerm = search,
            Items = items.Select(ToListItemViewModel).ToList()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var viewModel = await LoadDetailsViewModel(id);
        if (viewModel is null)
        {
            return NotFound();
        }

        ViewBag.MovementForm = new StockMovementFormViewModel
        {
            InventoryItemId = id,
            MovementType = StockMovementType.Entry,
            Quantity = 1
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterMovement(StockMovementFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var viewModelInvalid = await LoadDetailsViewModel(form.InventoryItemId);
            if (viewModelInvalid is null)
            {
                return NotFound();
            }

            ViewBag.MovementForm = form;
            return View("Details", viewModelInvalid);
        }

        var dto = new StockMovementCreateDto
        {
            InventoryItemId = form.InventoryItemId,
            MovementType = form.MovementType,
            Quantity = form.Quantity,
            Reference = form.Reference,
            Notes = form.Notes
        };

        try
        {
            await _inventoryService.RegisterMovementAsync(dto);
            TempData["Success"] = "Movimentação registrada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = form.InventoryItemId });
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }
        catch (NotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        var viewModel = await LoadDetailsViewModel(form.InventoryItemId);
        if (viewModel is null)
        {
            return NotFound();
        }

        ViewBag.MovementForm = form;
        return View("Details", viewModel);
    }

    private async Task<InventoryDetailsViewModel?> LoadDetailsViewModel(Guid id)
    {
        var item = await _inventoryService.GetByIdAsync(id);
        if (item is null)
        {
            return null;
        }

        return new InventoryDetailsViewModel
        {
            Id = item.Id,
            EpiTypeId = item.EpiTypeId,
            EpiCode = item.EpiCode,
            EpiDescription = item.EpiDescription,
            QuantityAvailable = item.QuantityAvailable,
            MinimumQuantity = item.MinimumQuantity,
            Location = item.Location,
            LastInventoryDate = item.LastInventoryDate,
            RecentMovements = item.RecentMovements
                .Select(movement => new StockMovementViewModel(
                    movement.Id,
                    movement.MovementType,
                    movement.Quantity,
                    movement.MovementDate,
                    movement.Reference,
                    movement.Notes))
                .ToList()
        };
    }

    private static InventoryListItemViewModel ToListItemViewModel(EPIFlow.Application.Inventory.DTOs.InventoryItemDto item)
    {
        var isBelowMinimum = item.QuantityAvailable <= item.MinimumQuantity;
        return new InventoryListItemViewModel(
            item.Id,
            item.EpiTypeId,
            item.EpiCode,
            item.EpiDescription,
            item.QuantityAvailable,
            item.MinimumQuantity,
            item.Location,
            item.LastInventoryDate,
            isBelowMinimum);
    }
}
