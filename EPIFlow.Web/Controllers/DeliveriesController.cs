using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Deliveries.DTOs.Requests;
using EPIFlow.Application.Deliveries.Services;
using EPIFlow.Application.Employees.Services;
using EPIFlow.Application.EpiTypes.Services;
using EPIFlow.Domain.Constants;
using EPIFlow.Domain.Enums;
using EPIFlow.Web.Models.Deliveries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator + "," + SystemRoles.Warehouse)]
public class DeliveriesController : Controller
{
    private readonly IDeliveryService _deliveryService;
    private readonly IEmployeeService _employeeService;
    private readonly IEpiTypeService _epiTypeService;

    public DeliveriesController(
        IDeliveryService deliveryService,
        IEmployeeService employeeService,
        IEpiTypeService epiTypeService)
    {
        _deliveryService = deliveryService;
        _employeeService = employeeService;
        _epiTypeService = epiTypeService;
    }

    public async Task<IActionResult> Index(Guid? employeeId, Guid? epiTypeId)
    {
        var deliveries = await _deliveryService.GetAllAsync(employeeId, epiTypeId);
        var employees = await _employeeService.GetAllAsync(null);
        var epiTypes = await _epiTypeService.GetAllAsync(null);

        var viewModel = new DeliveryIndexViewModel
        {
            SelectedEmployeeId = employeeId,
            SelectedEpiTypeId = epiTypeId,
            Employees = employees
                .Where(employee => employee.Status == EmployeeStatus.Active)
                .OrderBy(employee => employee.Name)
                .Select(employee => new SelectListItem(employee.Name, employee.Id.ToString(), employeeId.HasValue && employee.Id == employeeId.Value))
                .Prepend(new SelectListItem("Todos", string.Empty, !employeeId.HasValue)),
            EpiTypes = epiTypes
                .Where(epi => epi.IsActive)
                .OrderBy(epi => epi.Code)
                .Select(epi => new SelectListItem($"{epi.Code} - {epi.Description}", epi.Id.ToString(), epiTypeId.HasValue && epi.Id == epiTypeId.Value))
                .Prepend(new SelectListItem("Todos", string.Empty, !epiTypeId.HasValue)),
            Deliveries = deliveries
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
                .ToList()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        var viewModel = new DeliveryCreateViewModel();
        await PopulateCreateViewModelAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeliveryCreateViewModel viewModel)
    {
        viewModel.Items ??= new List<DeliveryItemInputModel>();
        viewModel.Items = viewModel.Items
            .Where(item => item.EpiTypeId != Guid.Empty)
            .ToList();

        if (!viewModel.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Informe ao menos um EPI para entrega.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateViewModelAsync(viewModel);
            return View(viewModel);
        }

        var dto = new DeliveryCreateDto
        {
            EmployeeId = viewModel.EmployeeId!.Value,
            DeliveryDate = viewModel.DeliveryDate,
            DeliveryNumber = viewModel.DeliveryNumber,
            Notes = viewModel.Notes,
            Items = viewModel.Items
                .Select(item => new DeliveryItemCreateDto
                {
                    EpiTypeId = item.EpiTypeId,
                    Quantity = item.Quantity,
                    ValidUntil = item.ValidUntil
                }).ToList()
        };

        try
        {
            await _deliveryService.RegisterDeliveryAsync(dto);
            TempData["Success"] = "Entrega registrada com sucesso.";
            return RedirectToAction(nameof(Index));
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

        await PopulateCreateViewModelAsync(viewModel);
        return View(viewModel);
    }

    public async Task<IActionResult> Return(Guid id)
    {
        var delivery = await _deliveryService.GetByIdAsync(id);
        if (delivery is null)
        {
            return NotFound();
        }

        var viewModel = new DeliveryReturnViewModel
        {
            DeliveryId = delivery.Id,
            EmployeeName = delivery.EmployeeName,
            Items = delivery.Items.Select(item => new DeliveryReturnItemViewModel
            {
                DeliveryItemId = item.Id,
                EpiDescription = $"{item.EpiCode} - {item.EpiDescription}",
                QuantityDelivered = item.Quantity,
                ReturnedQuantity = item.ReturnedQuantity,
                ReturnedAtUtc = item.ReturnedAtUtc,
                QuantityToReturn = Math.Max(0, item.Quantity - (item.ReturnedQuantity ?? 0))
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(DeliveryReturnViewModel viewModel)
    {
        viewModel.Items ??= new List<DeliveryReturnItemViewModel>();
        var itemsToReturn = viewModel.Items
            .Where(item => item.QuantityToReturn > 0)
            .Select(item => new DeliveryReturnDto
            {
                DeliveryItemId = item.DeliveryItemId,
                ReturnedQuantity = item.QuantityToReturn,
                ReturnedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (!itemsToReturn.Any())
        {
            ModelState.AddModelError(string.Empty, "Informe a quantidade para devolução.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            await _deliveryService.ReturnItemsAsync(itemsToReturn, HttpContext.RequestAborted);
            TempData["Success"] = "Devolução registrada com sucesso.";
            return RedirectToAction(nameof(Index));
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

        return View(viewModel);
    }

    private async Task PopulateCreateViewModelAsync(DeliveryCreateViewModel viewModel)
    {
        var employees = await _employeeService.GetAllAsync(null);
        var epiTypes = await _epiTypeService.GetAllAsync(null);

        viewModel.Employees = employees
            .Where(employee => employee.Status == EmployeeStatus.Active)
            .OrderBy(employee => employee.Name)
            .Select(employee => new SelectListItem(employee.Name, employee.Id.ToString()))
            .ToList();

        viewModel.EpiTypes = epiTypes
            .Where(epi => epi.IsActive)
            .OrderBy(epi => epi.Code)
            .Select(epi => new SelectListItem($"{epi.Code} - {epi.Description}", epi.Id.ToString()))
            .ToList();
    }
}
