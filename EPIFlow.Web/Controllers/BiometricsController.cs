using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Biometrics.DTOs;
using EPIFlow.Application.Biometrics.Services;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Domain.Constants;
using EPIFlow.Domain.Enums;
using EPIFlow.Web.Models.Biometrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = $"{SystemRoles.Administrator},{SystemRoles.Warehouse}")]
[Route("biometria")]
public class BiometricsController : Controller
{
    private readonly IBiometricEnrollmentService _biometricEnrollmentService;

    public BiometricsController(IBiometricEnrollmentService biometricEnrollmentService)
    {
        _biometricEnrollmentService = biometricEnrollmentService;
    }

    [HttpGet("colaboradores/{employeeId:guid}/overview")]
    public async Task<IActionResult> GetEmployeeOverview(Guid employeeId, CancellationToken cancellationToken)
    {
        try
        {
            var overview = await _biometricEnrollmentService.GetEmployeeOverviewAsync(employeeId, cancellationToken);
            var viewModel = MapToViewModel(overview);
            return Json(viewModel);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
    }

    [HttpPost("colaboradores/{employeeId:guid}/enroll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestEnrollment(Guid employeeId, [FromBody] FingerprintEnrollmentRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Verifique os dados informados." });
        }

        if (!TryParseFinger(model.Finger, out var finger))
        {
            return BadRequest(new { error = "Dedo informado inv\u00E1lido." });
        }

        try
        {
            var taskId = await _biometricEnrollmentService.RequestEnrollmentAsync(
                new FingerprintEnrollmentRequestDto(employeeId, model.AgentId, finger),
                cancellationToken);

            return Ok(new { success = true, taskId });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("colaboradores/{employeeId:guid}/test")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestVerification(Guid employeeId, [FromBody] FingerprintVerificationRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Verifique os dados informados." });
        }

        if (!TryParseFinger(model.Finger, out var finger))
        {
            return BadRequest(new { error = "Dedo informado inv\u00E1lido." });
        }

        try
        {
            var taskId = await _biometricEnrollmentService.RequestVerificationAsync(
                new FingerprintVerificationRequestDto(employeeId, model.AgentId, finger),
                cancellationToken);

            return Ok(new { success = true, taskId });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("colaboradores/{employeeId:guid}/enrollments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearEnrollments(Guid employeeId, CancellationToken cancellationToken)
    {
        try
        {
            await _biometricEnrollmentService.ClearEnrollmentsAsync(employeeId, cancellationToken);
            return Ok(new { success = true });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private static bool TryParseFinger(string value, out FingerType finger)
    {
        if (Enum.TryParse<FingerType>(value, ignoreCase: true, out finger))
        {
            return true;
        }

        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(FingerType), numeric))
        {
            finger = (FingerType)numeric;
            return true;
        }

        finger = default;
        return false;
    }

    private static EmployeeBiometricOverviewViewModel MapToViewModel(EmployeeBiometricOverviewDto dto)
    {
        var viewModel = new EmployeeBiometricOverviewViewModel
        {
            EmployeeId = dto.EmployeeId,
            EmployeeName = dto.EmployeeName,
            Agents = dto.Agents
                .Select(agent => new BiometricAgentViewModel
                {
                    Id = agent.Id,
                    Name = agent.Name,
                    Description = agent.Description,
                    IsActive = agent.IsActive,
                    IsOnline = agent.IsOnline,
                    LastSeenAtUtc = agent.LastSeenAtUtc,
                    PollingIntervalSeconds = agent.PollingIntervalSeconds
                })
                .ToList(),
            Fingers = dto.Fingers
                .Select(finger => new FingerStatusViewModel
                {
                    FingerValue = (int)finger.Finger,
                    FingerKey = finger.Finger.ToString(),
                    DisplayName = finger.DisplayName,
                    StatusValue = finger.Status?.ToString(),
                    StatusLabel = finger.StatusLabel,
                    RequestedAtUtc = finger.RequestedAtUtc,
                    UpdatedAtUtc = finger.UpdatedAtUtc,
                    TaskId = finger.TaskId,
                    CanTest = finger.HasTemplate
                })
                .ToList()
        };

        return viewModel;
    }
}
