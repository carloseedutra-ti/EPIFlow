using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Biometrics.DTOs;
using EPIFlow.Application.Biometrics.Services;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Domain.Constants;
using EPIFlow.Web.Models.Biometrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator)]
public class BiometricAgentsController : Controller
{
    private readonly IBiometricEnrollmentService _biometricEnrollmentService;

    public BiometricAgentsController(IBiometricEnrollmentService biometricEnrollmentService)
    {
        _biometricEnrollmentService = biometricEnrollmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildIndexViewModelAsync(null, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BiometricAgentFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildIndexViewModelAsync(form, cancellationToken);
            return View("Index", model);
        }

        try
        {
            var apiKey = await _biometricEnrollmentService.CreateAgentAsync(new BiometricAgentCreateDto
            {
                Name = form.Name,
                MachineName = form.MachineName,
                Description = form.Description,
                PollingIntervalSeconds = form.PollingIntervalSeconds
            }, cancellationToken);

            TempData["Success"] = $"Agente criado com sucesso. Chave de acesso: {apiKey}.";
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

            var model = await BuildIndexViewModelAsync(form, cancellationToken);
            TempData["Error"] = ex.Message;
            return View("Index", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _biometricEnrollmentService.SetAgentStatusAsync(id, isActive, cancellationToken);
            TempData["Success"] = "Status do agente atualizado.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = await _biometricEnrollmentService.ResetAgentKeyAsync(id, cancellationToken);
            TempData["Success"] = $"Nova chave gerada: {apiKey}";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<BiometricAgentIndexViewModel> BuildIndexViewModelAsync(BiometricAgentFormViewModel? form, CancellationToken cancellationToken)
    {
        var agents = await _biometricEnrollmentService.GetAgentsAsync(cancellationToken);

        var model = new BiometricAgentIndexViewModel
        {
            Agents = agents
                .Select(agent => new BiometricAgentItemViewModel
                {
                    Id = agent.Id,
                    Name = agent.Name,
                    Description = agent.Description,
                    MachineName = agent.MachineName,
                    IsActive = agent.IsActive,
                    IsOnline = agent.IsOnline,
                    LastSeenAtUtc = agent.LastSeenAtUtc,
                    ApiKey = agent.ApiKey,
                    PollingIntervalSeconds = agent.PollingIntervalSeconds
                })
                .OrderByDescending(agent => agent.IsActive)
                .ThenBy(agent => agent.Name)
                .ToList(),
            Form = form ?? new BiometricAgentFormViewModel()
        };

        return model;
    }
}
