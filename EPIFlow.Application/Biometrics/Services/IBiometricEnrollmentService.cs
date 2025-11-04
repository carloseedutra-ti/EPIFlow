using System;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Biometrics.DTOs;

namespace EPIFlow.Application.Biometrics.Services;

public interface IBiometricEnrollmentService
{
    Task<IReadOnlyList<BiometricAgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateAgentAsync(BiometricAgentCreateDto dto, CancellationToken cancellationToken = default);

    Task SetAgentStatusAsync(Guid agentId, bool isActive, CancellationToken cancellationToken = default);

    Task<Guid> ResetAgentKeyAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<BiometricAgentDto?> GetAgentConfigurationAsync(Guid apiKey, CancellationToken cancellationToken = default);


    Task CompleteTaskAsync(Guid apiKey, Guid taskId, string templateBase64, CancellationToken cancellationToken = default);

    Task FailTaskAsync(Guid apiKey, Guid taskId, string reason, CancellationToken cancellationToken = default);

    Task<EmployeeBiometricOverviewDto> GetEmployeeOverviewAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<Guid> RequestEnrollmentAsync(FingerprintEnrollmentRequestDto request, CancellationToken cancellationToken = default);

    Task<Guid> RequestVerificationAsync(FingerprintVerificationRequestDto request, CancellationToken cancellationToken = default);

    Task<BiometricTaskPayloadDto?> DequeueTaskAsync(Guid apiKey, CancellationToken cancellationToken = default);

    Task ClearEnrollmentsAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<BiometricTaskStatusDto?> GetTaskStatusAsync(Guid taskId, CancellationToken cancellationToken = default);
}
