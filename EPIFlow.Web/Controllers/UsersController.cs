using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Biometrics.Services;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Users.DTOs;
using EPIFlow.Domain.Constants;
using EPIFlow.Web.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator)]
public class UsersController : Controller
{
    private static readonly string[] AvailableRoles =
    {
        SystemRoles.Administrator,
        SystemRoles.Warehouse,
        SystemRoles.User
    };

    private static readonly Dictionary<string, string> RoleDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        [SystemRoles.Administrator] = "Administrador",
        [SystemRoles.Warehouse] = "Almoxarifado",
        [SystemRoles.User] = "Usu\u00E1rio"
    };

    private readonly IUserManagementService _userManagementService;
    private readonly IBiometricEnrollmentService _biometricEnrollmentService;

    public UsersController(
        IUserManagementService userManagementService,
        IBiometricEnrollmentService biometricEnrollmentService)
    {
        _userManagementService = userManagementService;
        _biometricEnrollmentService = biometricEnrollmentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userManagementService.GetUsersAsync(cancellationToken);
        var viewModel = users
            .Select(user => new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Department = user.Department,
                JobTitle = user.JobTitle,
                IsActive = user.IsActive,
                DefaultAgentName = user.DefaultBiometricAgentName,
                RolesDescription = FormatRoles(user.Roles)
            })
            .ToList();

        return View(viewModel);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new UserFormViewModel
        {
            RequirePassword = true,
            IsActive = true,
            SelectedRoles = new List<string> { SystemRoles.User }
        };

        await PopulateFormOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel form, CancellationToken cancellationToken)
    {
        form.RequirePassword = true;

        if (string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError(nameof(form.Password), "Informe a senha inicial do usu\u00E1rio.");
        }

        if (form.SelectedRoles == null || form.SelectedRoles.Count == 0)
        {
            ModelState.AddModelError(nameof(form.SelectedRoles), "Selecione pelo menos uma permiss\u00E3o.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(form, cancellationToken);
            return View(form);
        }

        var selectedRoles = form.SelectedRoles ?? new List<string>();
        form.SelectedRoles = selectedRoles;

        var dto = new UserCreateDto(
            form.Email.Trim(),
            form.Password!,
            form.FullName?.Trim(),
            form.Department?.Trim(),
            form.JobTitle?.Trim(),
            form.DefaultBiometricAgentId,
            selectedRoles,
            form.IsActive);

        try
        {
            await _userManagementService.CreateAsync(dto, cancellationToken);
            TempData["Success"] = "Usu\u00E1rio criado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        await PopulateFormOptionsAsync(form, cancellationToken);
        return View(form);
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var model = new UserFormViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Department = user.Department,
            JobTitle = user.JobTitle,
            DefaultBiometricAgentId = user.DefaultBiometricAgentId,
            IsActive = user.IsActive,
            SelectedRoles = user.Roles.ToList(),
            RequirePassword = false
        };

        await PopulateFormOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserFormViewModel form, CancellationToken cancellationToken)
    {
        form.RequirePassword = false;

        if (form.SelectedRoles == null || form.SelectedRoles.Count == 0)
        {
            ModelState.AddModelError(nameof(form.SelectedRoles), "Selecione pelo menos uma permiss\u00E3o.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(form, cancellationToken);
            return View(form);
        }

        var selectedRoles = form.SelectedRoles ?? new List<string>();
        form.SelectedRoles = selectedRoles;

        var dto = new UserUpdateDto(
            form.Email.Trim(),
            form.FullName?.Trim(),
            form.Department?.Trim(),
            form.JobTitle?.Trim(),
            form.DefaultBiometricAgentId,
            selectedRoles,
            form.IsActive,
            string.IsNullOrWhiteSpace(form.Password) ? null : form.Password);

        try
        {
            await _userManagementService.UpdateAsync(id, dto, cancellationToken);
            TempData["Success"] = "Usu\u00E1rio atualizado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        await PopulateFormOptionsAsync(form, cancellationToken);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.SetActiveAsync(id, isActive, cancellationToken);
            TempData["Success"] = isActive
                ? "Usu\u00E1rio reativado com sucesso."
                : "Usu\u00E1rio desativado com sucesso.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFormOptionsAsync(UserFormViewModel model, CancellationToken cancellationToken)
    {
        var selectedRoles = model.SelectedRoles?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (selectedRoles.Count == 0)
        {
            selectedRoles.Add(SystemRoles.User);
            model.SelectedRoles = selectedRoles.ToList();
        }

        model.RoleOptions = AvailableRoles
            .Select(role => new SelectListItem
            {
                Value = role,
                Text = GetRoleDisplayName(role),
                Selected = selectedRoles.Contains(role)
            })
            .ToList();

        var agents = await _biometricEnrollmentService.GetAgentsAsync(cancellationToken);
        var agentOptions = new List<SelectListItem>
        {
            new("Selecione", string.Empty)
        };

        foreach (var agent in agents)
        {
            var label = agent.IsOnline ? $"{agent.Name} (online)" : agent.Name;
            agentOptions.Add(new SelectListItem
            {
                Value = agent.Id.ToString(),
                Text = label,
                Selected = model.DefaultBiometricAgentId.HasValue && model.DefaultBiometricAgentId.Value == agent.Id
            });
        }

        model.AgentOptions = agentOptions;
    }

    private static string FormatRoles(IEnumerable<string> roles)
    {
        var ordered = AvailableRoles
            .Where(role => roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .Select(GetRoleDisplayName)
            .ToList();

        var others = roles
            .Where(role => !AvailableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .OrderBy(role => role)
            .Select(GetRoleDisplayName)
            .ToList();

        ordered.AddRange(others);
        return string.Join(", ", ordered);
    }

    private static string GetRoleDisplayName(string role)
    {
        return RoleDisplayNames.TryGetValue(role, out var displayName)
            ? displayName
            : role;
    }
}
