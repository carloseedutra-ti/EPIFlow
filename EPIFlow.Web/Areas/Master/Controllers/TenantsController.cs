using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Tenants.DTOs;
using EPIFlow.Application.Tenants.Services;
using EPIFlow.Web.Areas.Master.Models.Tenants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Areas.Master.Controllers;

public class TenantsController : MasterControllerBase
{
    private readonly ITenantManagementService _tenantManagementService;
    private readonly IWebHostEnvironment _environment;

    public TenantsController(ITenantManagementService tenantManagementService, IWebHostEnvironment environment)
    {
        _tenantManagementService = tenantManagementService;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var tenants = await _tenantManagementService.GetAllAsync(search);
        var viewModel = new TenantIndexViewModel
        {
            SearchTerm = search,
            Tenants = tenants
        };
        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new CreateTenantRequest
        {
            Country = "Brasil",
            EmployeeLimit = 10
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateTenantRequest request)
    {
        request.LegalName = request.Name;

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();

            return BadRequest(new { error = errors.Any() ? string.Join(Environment.NewLine, errors) : "Verifique os dados informados." });
        }

        string? logoPath = null;
        string? physicalLogoPath = null;
        if (request.Logo is { Length: > 0 })
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "logotipo");
            Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(request.Logo.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            physicalLogoPath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(physicalLogoPath, FileMode.Create);
            await request.Logo.CopyToAsync(stream);

            logoPath = $"/uploads/logotipo/{fileName}";
        }

        try
        {
            var dto = request.ToDto(logoPath);
            await _tenantManagementService.CreateAsync(dto);
            return Ok(new { success = true });
        }
        catch (ConflictException ex)
        {
            if (physicalLogoPath is not null && System.IO.File.Exists(physicalLogoPath))
            {
                System.IO.File.Delete(physicalLogoPath);
            }
            return BadRequest(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            if (physicalLogoPath is not null && System.IO.File.Exists(physicalLogoPath))
            {
                System.IO.File.Delete(physicalLogoPath);
            }
            var errors = ex.Errors?.SelectMany(kvp => kvp.Value).ToList();
            var message = errors != null && errors.Any() ? string.Join(Environment.NewLine, errors) : ex.Message;
            return BadRequest(new { error = message });
        }
        catch (Exception ex)
        {
            if (physicalLogoPath is not null && System.IO.File.Exists(physicalLogoPath))
            {
                System.IO.File.Delete(physicalLogoPath);
            }
            var message = $"Não foi possível criar a empresa. {ex.Message}";
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                message += Environment.NewLine + ex.StackTrace;
            }

            return StatusCode(500, new { error = message });
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var tenant = await _tenantManagementService.GetByIdAsync(id);
        if (tenant is null)
        {
            return NotFound();
        }

        ViewBag.PaymentModel = new TenantPaymentCreateDto
        {
            TenantId = id,
            PaymentDate = DateTime.Today,
            Amount = 0
        };

        return View(tenant);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var tenant = await _tenantManagementService.GetByIdAsync(id);
        if (tenant is null)
        {
            return NotFound();
        }

        var model = new TenantUpdateDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            LegalName = tenant.LegalName,
            Document = tenant.Document,
            PhoneNumber = tenant.PhoneNumber,
            Address = tenant.Address,
            AddressComplement = tenant.AddressComplement,
            City = tenant.City,
            State = tenant.State,
            PostalCode = tenant.PostalCode,
            Country = tenant.Country,
            ResponsibleName = tenant.ResponsibleName,
            ResponsibleEmail = tenant.ResponsibleEmail,
            EmployeeLimit = tenant.EmployeeLimit,
            IsActive = tenant.IsActive,
            IsSuspended = tenant.IsSuspended,
            SubscriptionExpiresOnUtc = tenant.SubscriptionExpiresOnUtc,
            Notes = tenant.Notes,
            LogoPath = tenant.LogoPath
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TenantUpdateDto model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _tenantManagementService.UpdateAsync(model);
            TempData["Success"] = "Dados da empresa atualizados.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive)
    {
        try
        {
            await _tenantManagementService.SetActiveAsync(id, isActive);
            TempData["Success"] = "Status atualizado.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSuspension(Guid id, bool isSuspended)
    {
        try
        {
            await _tenantManagementService.SetSuspendedAsync(id, isSuspended);
            TempData["Success"] = isSuspended ? "Empresa suspensa." : "Empresa reativada.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPayment(TenantPaymentCreateDto model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Verifique os dados do pagamento.";
            return RedirectToAction(nameof(Details), new { id = model.TenantId });
        }

        try
        {
            await _tenantManagementService.AddPaymentAsync(model);
            TempData["Success"] = "Pagamento registrado.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.TenantId });
    }
}
