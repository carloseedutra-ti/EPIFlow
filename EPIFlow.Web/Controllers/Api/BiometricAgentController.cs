using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Biometrics.Services;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Web.Models.Biometrics;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Controllers.Api;

[ApiController]
[Route("api/biometria/agent")]
public class BiometricAgentController : ControllerBase
{
    private readonly IBiometricEnrollmentService _biometricEnrollmentService;

    public BiometricAgentController(IBiometricEnrollmentService biometricEnrollmentService)
    {
        _biometricEnrollmentService = biometricEnrollmentService;
    }

    [HttpPost("config")]
    public async Task<IActionResult> Config([FromBody] AgentPollRequestModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AgentConfigurationResponseModel
            {
                Success = false,
                Message = "Informe a chave do agente."
            });
        }

        var configuration = await _biometricEnrollmentService.GetAgentConfigurationAsync(request.ApiKey, cancellationToken);

        if (configuration is null)
        {
            return NotFound(new AgentConfigurationResponseModel
            {
                Success = false,
                Message = "Agente nao encontrado."
            });
        }

        if (!configuration.IsActive)
        {
            return Unauthorized(new AgentConfigurationResponseModel
            {
                Success = false,
                Message = "Agente desativado."
            });
        }

        return Ok(new AgentConfigurationResponseModel
        {
            Success = true,
            AgentName = configuration.Name,
            PollingIntervalSeconds = configuration.PollingIntervalSeconds
        });
    }

    [HttpPost("poll")]
    public async Task<IActionResult> Poll([FromBody] AgentPollRequestModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Informe a chave do agente." });
        }

        try
        {
            var task = await _biometricEnrollmentService.DequeueTaskAsync(request.ApiKey, cancellationToken);

            if (task is null)
            {
                return NoContent();
            }

            Dictionary<string, object?> payloadObject;
            try
            {
                payloadObject = JsonSerializer.Deserialize<Dictionary<string, object?>>(task.PayloadJson) ?? new Dictionary<string, object?>();
            }
            catch (JsonException)
            {
                payloadObject = new Dictionary<string, object?>
                {
                    { "raw", task.PayloadJson }
                };
            }

            var response = new AgentTaskResponseModel
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                EmployeeName = task.EmployeeName,
                EmployeeRegistrationNumber = task.EmployeeRegistrationNumber,
                Finger = (int)task.Finger,
                FingerName = task.Finger.ToString(),
                Payload = payloadObject
            };

            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] AgentTaskCompletionRequestModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Verifique os dados informados." });
        }

        try
        {
            await _biometricEnrollmentService.CompleteTaskAsync(
                request.ApiKey,
                request.TaskId,
                request.TemplateBase64 ?? string.Empty,
                cancellationToken);
            return Ok(new { success = true });
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

    [HttpPost("fail")]
    public async Task<IActionResult> Fail([FromBody] AgentTaskFailureRequestModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Verifique os dados informados." });
        }

        try
        {
            await _biometricEnrollmentService.FailTaskAsync(request.ApiKey, request.TaskId, request.Reason ?? string.Empty, cancellationToken);
            return Ok(new { success = true });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
