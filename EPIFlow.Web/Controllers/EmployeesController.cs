using System;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Employees.DTOs;
using EPIFlow.Application.Employees.Services;
using EPIFlow.Domain.Constants;
using EPIFlow.Web.Models.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator)]
public class EmployeesController : Controller
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var employees = await _employeeService.GetAllAsync(search);

        var viewModel = new EmployeeIndexViewModel
        {
            SearchTerm = search,
            Employees = employees
                .Select(employee => new EmployeeListItemViewModel(
                    employee.Id,
                    employee.Name,
                    employee.Cpf,
                    employee.RegistrationNumber,
                    employee.JobTitle,
                    employee.Department,
                    employee.AdmissionDate,
                    employee.Status,
                    employee.TotalDeliveries))
                .ToList()
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        var viewModel = new EmployeeFormViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var dto = new EmployeeCreateDto
        {
            Name = viewModel.Name,
            Cpf = viewModel.Cpf,
            RegistrationNumber = viewModel.RegistrationNumber,
            JobTitle = viewModel.JobTitle,
            Department = viewModel.Department,
            AdmissionDate = viewModel.AdmissionDate
        };

        try
        {
            await _employeeService.CreateAsync(dto);
            TempData["Success"] = "Colaborador cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        var viewModel = new EmployeeFormViewModel
        {
            Id = employee.Id,
            Name = employee.Name,
            Cpf = employee.Cpf,
            RegistrationNumber = employee.RegistrationNumber,
            JobTitle = employee.JobTitle,
            Department = employee.Department,
            AdmissionDate = employee.AdmissionDate,
            Status = employee.Status
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EmployeeFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var dto = new EmployeeUpdateDto
        {
            Id = viewModel.Id!.Value,
            Name = viewModel.Name,
            Cpf = viewModel.Cpf,
            RegistrationNumber = viewModel.RegistrationNumber,
            JobTitle = viewModel.JobTitle,
            Department = viewModel.Department,
            AdmissionDate = viewModel.AdmissionDate,
            Status = viewModel.Status
        };

        try
        {
            await _employeeService.UpdateAsync(dto);
            TempData["Success"] = "Dados do colaborador atualizados.";
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
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _employeeService.DeleteAsync(id);
            TempData["Success"] = "Colaborador removido.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
