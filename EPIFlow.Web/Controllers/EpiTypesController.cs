using System;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.EpiTypes.DTOs;
using EPIFlow.Application.EpiTypes.Services;
using EPIFlow.Domain.Constants;
using EPIFlow.Web.Models.EpiTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator + "," + SystemRoles.Warehouse)]
public class EpiTypesController : Controller
{
    private readonly IEpiTypeService _epiTypeService;

    public EpiTypesController(IEpiTypeService epiTypeService)
    {
        _epiTypeService = epiTypeService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var epiTypes = await _epiTypeService.GetAllAsync(search);

        var viewModel = new EpiTypeIndexViewModel
        {
            SearchTerm = search,
            EpiTypes = epiTypes
                .Select(epi => new EpiTypeListItemViewModel(
                    epi.Id,
                    epi.Code,
                    epi.Description,
                    epi.Category,
                    epi.ValidityInMonths,
                    epi.CaNumber,
                    epi.IsActive,
                    epi.QuantityAvailable,
                    epi.MinimumQuantity))
                .ToList()
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new EpiTypeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EpiTypeFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var dto = new EpiTypeCreateDto
        {
            Code = viewModel.Code,
            Description = viewModel.Description,
            Category = viewModel.Category,
            ValidityInMonths = viewModel.ValidityInMonths,
            CaNumber = viewModel.CaNumber,
            MinimumQuantity = viewModel.MinimumQuantity
        };

        try
        {
            await _epiTypeService.CreateAsync(dto);
            TempData["Success"] = "Tipo de EPI cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(viewModel);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var epiType = await _epiTypeService.GetByIdAsync(id);
        if (epiType is null)
        {
            return NotFound();
        }

        var viewModel = new EpiTypeFormViewModel
        {
            Id = epiType.Id,
            Code = epiType.Code,
            Description = epiType.Description,
            Category = epiType.Category,
            ValidityInMonths = epiType.ValidityInMonths,
            CaNumber = epiType.CaNumber,
            MinimumQuantity = epiType.MinimumQuantity,
            IsActive = epiType.IsActive
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EpiTypeFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var dto = new EpiTypeUpdateDto
        {
            Id = viewModel.Id!.Value,
            Code = viewModel.Code,
            Description = viewModel.Description,
            Category = viewModel.Category,
            ValidityInMonths = viewModel.ValidityInMonths,
            CaNumber = viewModel.CaNumber,
            MinimumQuantity = viewModel.MinimumQuantity,
            IsActive = viewModel.IsActive
        };

        try
        {
            await _epiTypeService.UpdateAsync(dto);
            TempData["Success"] = "Tipo de EPI atualizado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive)
    {
        try
        {
            await _epiTypeService.ToggleStatusAsync(id, isActive);
            TempData["Success"] = "Status do EPI atualizado.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
