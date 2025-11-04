using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Biometrics.DTOs;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Domain.Entities;
using EPIFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.Biometrics.Services;

public class BiometricEnrollmentService : IBiometricEnrollmentService
{
    private static readonly IReadOnlyDictionary<FingerType, string> FingerDisplayNames = new Dictionary<FingerType, string>
    {
        { FingerType.RightThumb, "Polegar direito" },
        { FingerType.RightIndex, "Indicador direito" },
        { FingerType.RightMiddle, "M\u00E9dio direito" },
        { FingerType.RightRing, "Anelar direito" },
        { FingerType.RightLittle, "M\u00EDnimo direito" },
        { FingerType.LeftThumb, "Polegar esquerdo" },
        { FingerType.LeftIndex, "Indicador esquerdo" },
        { FingerType.LeftMiddle, "M\u00E9dio esquerdo" },
        { FingerType.LeftRing, "Anelar esquerdo" },
        { FingerType.LeftLittle, "M\u00EDnimo esquerdo" }
    };

    private readonly IRepository<BiometricAgent> _agentRepository;
    private readonly IRepository<BiometricTask> _taskRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private static readonly TimeSpan CaptureTimeout = TimeSpan.FromMinutes(5);

    public BiometricEnrollmentService(
        IRepository<BiometricAgent> agentRepository,
        IRepository<BiometricTask> taskRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _agentRepository = agentRepository;
        _taskRepository = taskRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    private static BiometricAgentDto MapAgentToDto(BiometricAgent agent, DateTime referenceUtc)
    {
        var safeIntervalSeconds = Math.Max(agent.PollingIntervalSeconds, 5);
        var isOnline = agent.LastSeenAtUtc.HasValue &&
                       referenceUtc - agent.LastSeenAtUtc <= TimeSpan.FromSeconds(safeIntervalSeconds * 2);

        return new BiometricAgentDto(
            agent.Id,
            agent.Name,
            agent.Description,
            agent.MachineName,
            agent.IsActive,
            isOnline,
            agent.LastSeenAtUtc,
            agent.PollingIntervalSeconds,
            agent.ApiKey);
    }

    private static string? GetTaskOperation(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.TryGetProperty("operation", out var operationElement))
            {
                return operationElement.GetString();
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? ExtractTemplateBase64(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(resultJson);
            if (document.RootElement.TryGetProperty("templateBase64", out var templateElement))
            {
                var value = templateElement.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? ExtractTemplateFromPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.TryGetProperty("templateBase64", out var templateElement))
            {
                var value = templateElement.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<BiometricAgent> GetAgentByApiKeyAsync(Guid apiKey, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.AsQueryable()
            .FirstOrDefaultAsync(a => a.ApiKey == apiKey, cancellationToken);

        if (agent is null)
        {
            throw new NotFoundException("Agente nÃƒÂ£o encontrado.");
        }

        agent.LastSeenAtUtc = _dateTimeProvider.UtcNow;
        return agent;
    }

    public async Task<IReadOnlyList<BiometricAgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        var agents = await _agentRepository.AsQueryable()
            .OrderBy(agent => agent.Name)
            .ToListAsync(cancellationToken);

        return agents
            .Select(agent => MapAgentToDto(agent, now))
            .ToList();
    }

    public async Task CompleteTaskAsync(Guid apiKey, Guid taskId, string templateBase64, CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentByApiKeyAsync(apiKey, cancellationToken);

        var task = await _taskRepository.AsQueryable()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.BiometricAgentId == agent.Id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Tarefa biométrica não encontrada.");
        }

        if (task.Status is BiometricTaskStatus.Completed)
        {
            return;
        }

        var operation = GetTaskOperation(task.PayloadJson);
        var isVerification = string.Equals(operation, "verify", StringComparison.OrdinalIgnoreCase);

        if (!isVerification && string.IsNullOrWhiteSpace(templateBase64))
        {
            throw new ValidationException(
                "Template invalido.",
                new Dictionary<string, string[]> { { "templateBase64", new[] { "Informe o template em Base64." } } });
        }

        var utcNow = _dateTimeProvider.UtcNow;

        task.Status = BiometricTaskStatus.Completed;
        task.CompletedAtUtc = utcNow;
        task.CompletedByUserName = agent.Name;
        task.CompletedByUserId = agent.Id;
        task.FailureReason = null;
        var finalTemplate = templateBase64;
        if (isVerification && string.IsNullOrWhiteSpace(finalTemplate))
        {
            finalTemplate = ExtractTemplateFromPayload(task.PayloadJson);
        }
        task.ResultJson = JsonSerializer.Serialize(new
        {
            templateBase64 = finalTemplate,
            operation,
            verified = isVerification
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task FailTaskAsync(Guid apiKey, Guid taskId, string reason, CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentByApiKeyAsync(apiKey, cancellationToken);

        var task = await _taskRepository.AsQueryable()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.BiometricAgentId == agent.Id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Tarefa biomÃƒÂ©trica nÃƒÂ£o encontrada.");
        }

        var utcNow = _dateTimeProvider.UtcNow;

        task.Status = BiometricTaskStatus.Failed;
        task.CompletedAtUtc = utcNow;
        task.CompletedByUserName = agent.Name;
        task.CompletedByUserId = agent.Id;
        task.FailureReason = string.IsNullOrWhiteSpace(reason) ? "Falha reportada pelo agente." : reason.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateAgentAsync(BiometricAgentCreateDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException(
                "NÃƒÂ£o foi possÃƒÂ­vel identificar a empresa.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "A sessÃƒÂ£o expirou. FaÃƒÂ§a login novamente." } } });
        }

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(
                "Informe o nome do agente.",
                new Dictionary<string, string[]> { { "Name", new[] { "O nome do agente ÃƒÂ© obrigatÃƒÂ³rio." } } });
        }

        var exists = await _agentRepository.AsQueryable()
            .AnyAsync(agent => agent.Name == name, cancellationToken);

        if (exists)
        {
            throw new ValidationException(
                "JÃƒÂ¡ existe um agente com este nome.",
                new Dictionary<string, string[]> { { "Name", new[] { "Utilize um nome diferente para o agente." } } });
        }

        var pollingInterval = dto.PollingIntervalSeconds < 3 ? 5 : dto.PollingIntervalSeconds;

        var agent = new BiometricAgent
        {
            Name = name,
            MachineName = dto.MachineName?.Trim(),
            Description = dto.Description?.Trim(),
            PollingIntervalSeconds = pollingInterval,
            TenantId = tenantId.Value
        };

        await _agentRepository.AddAsync(agent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return agent.ApiKey;
    }

    public async Task SetAgentStatusAsync(Guid agentId, bool isActive, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            throw new NotFoundException("Agente nÃƒÂ£o encontrado.");
        }

        agent.IsActive = isActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> ResetAgentKeyAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            throw new NotFoundException("Agente nÃƒÂ£o encontrado.");
        }

        agent.ApiKey = Guid.NewGuid();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return agent.ApiKey;
    }

    public async Task<BiometricAgentDto?> GetAgentConfigurationAsync(Guid apiKey, CancellationToken cancellationToken = default)
    {
        if (apiKey == Guid.Empty)
        {
            return null;
        }

        var agent = await _agentRepository.AsQueryable()
            .FirstOrDefaultAsync(a => a.ApiKey == apiKey, cancellationToken);

        if (agent is null)
        {
            return null;
        }

        var now = _dateTimeProvider.UtcNow;
        agent.LastSeenAtUtc = now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAgentToDto(agent, now);
    }

    public async Task<EmployeeBiometricOverviewDto> GetEmployeeOverviewAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException(
                "NÃƒÂ£o foi possÃƒÂ­vel identificar a empresa.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "A sessÃƒÂ£o expirou. FaÃƒÂ§a login novamente." } } });
        }

        var employee = await _employeeRepository.AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(emp => emp.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException("Colaborador nÃƒÂ£o encontrado.");
        }

        var now = _dateTimeProvider.UtcNow;

        var agents = await _agentRepository.AsQueryable()
            .AsNoTracking()
            .Where(agent => agent.IsActive)
            .OrderBy(agent => agent.Name)
            .ToListAsync(cancellationToken);

        var agentDtos = agents
            .Select(agent => MapAgentToDto(agent, now))
            .ToList();

        var tasks = await _taskRepository.AsQueryable()
            .AsNoTracking()
            .Where(task => task.EmployeeId == employeeId)
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var latestTaskByFinger = tasks
            .GroupBy(task => task.Finger)
            .ToDictionary(group => group.Key, group => group.First());

        var hasTemplateByFinger = tasks
            .Where(task =>
                task.Status == BiometricTaskStatus.Completed &&
                !string.IsNullOrWhiteSpace(ExtractTemplateBase64(task.ResultJson)))
            .GroupBy(task => task.Finger)
            .ToDictionary(group => group.Key, group => true);

        var fingerDtos = Enum.GetValues(typeof(FingerType))
            .Cast<FingerType>()
            .Select(finger =>
            {
                latestTaskByFinger.TryGetValue(finger, out var task);
                var operation = GetTaskOperation(task?.PayloadJson);

                BiometricTaskStatus? status = task?.Status;
                string statusLabel = status switch
                {
                    BiometricTaskStatus.Pending => "Pendente",
                    BiometricTaskStatus.InProgress => "Em captura",
                    BiometricTaskStatus.Completed => string.Equals(operation, "verify", StringComparison.OrdinalIgnoreCase)
                        ? "Teste concluido"
                        : "Concluido",
                    BiometricTaskStatus.Failed => "Falhou",
                    BiometricTaskStatus.Cancelled => "Cancelado",
                    _ => "Não iniciado"
                };

                var updatedAt = task?.CompletedAtUtc
                                ?? task?.UpdatedAtUtc
                                ?? task?.StartedAtUtc
                                ?? task?.AssignedAtUtc
                                ?? task?.CreatedAtUtc;

                return new FingerStatusDto(
                    finger,
                    FingerDisplayNames[finger],
                    status,
                    status.HasValue ? statusLabel : "Não iniciado",
                    task?.CreatedAtUtc,
                    updatedAt,
                    task?.Id,
                    hasTemplateByFinger.ContainsKey(finger));
            })
            .ToList();

        return new EmployeeBiometricOverviewDto(
            employee.Id,
            employee.Name,
            fingerDtos,
            agentDtos);
    }

    public async Task<Guid> RequestEnrollmentAsync(FingerprintEnrollmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException(
                "NÃƒÂ£o foi possÃƒÂ­vel identificar a empresa.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "A sessÃƒÂ£o expirou. FaÃƒÂ§a login novamente." } } });
        }

        var agent = await _agentRepository.GetByIdAsync(request.AgentId, cancellationToken);
        if (agent is null || !agent.IsActive)
        {
            throw new ValidationException(
                "Agente biomÃƒÂ©trico invÃƒÂ¡lido.",
                new Dictionary<string, string[]> { { "AgentId", new[] { "Selecione um agente ativo." } } });
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException("Colaborador nÃƒÂ£o encontrado.");
        }

        var hasPendingTask = await _taskRepository.AsQueryable()
            .AnyAsync(task =>
                task.EmployeeId == request.EmployeeId &&
                task.Finger == request.Finger &&
                (task.Status == BiometricTaskStatus.Pending || task.Status == BiometricTaskStatus.InProgress),
                cancellationToken);

        if (hasPendingTask)
        {
            throw new ValidationException(
                "JÃƒÂ¡ existe uma captura pendente para este dedo.",
                new Dictionary<string, string[]> { { "Finger", new[] { "Finalize ou cancele a captura atual antes de iniciar uma nova." } } });
        }

        var payload = JsonSerializer.Serialize(new
        {
            colaboradorId = employee.RegistrationNumber ?? employee.Id.ToString(),
            nome = employee.Name,
            operation = "enroll"
        });

        var task = new BiometricTask
        {
            BiometricAgentId = agent.Id,
            EmployeeId = employee.Id,
            Finger = request.Finger,
            Status = BiometricTaskStatus.Pending,
            EmployeeName = employee.Name,
            EmployeeRegistrationNumber = employee.RegistrationNumber,
            RequestedByUserId = _currentUserService.GetUserId(),
            RequestedByUserName = _currentUserService.GetUserName(),
            PayloadJson = payload,
            TenantId = employee.TenantId
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Id;
    }

    public async Task<Guid> RequestVerificationAsync(FingerprintVerificationRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException(
                "Não foi possível identificar a empresa.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "A sessao expirou. Faca login novamente." } } });
        }

        var agent = await _agentRepository.GetByIdAsync(request.AgentId, cancellationToken);
        if (agent is null || !agent.IsActive)
        {
            throw new ValidationException(
                "Agente biometrico invalido.",
                new Dictionary<string, string[]> { { "AgentId", new[] { "Selecione um agente ativo." } } });
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException("Colaborador não encontrado.");
        }

        var latestTemplateJson = await _taskRepository.AsQueryable()
            .Where(task =>
                task.EmployeeId == request.EmployeeId &&
                task.Finger == request.Finger &&
                task.Status == BiometricTaskStatus.Completed &&
                task.ResultJson != null)
            .OrderByDescending(task =>
                task.CompletedAtUtc ??
                task.UpdatedAtUtc ??
                task.CreatedAtUtc)
            .Select(task => task.ResultJson)
            .FirstOrDefaultAsync(cancellationToken);

        var templateBase64 = ExtractTemplateBase64(latestTemplateJson);
        if (string.IsNullOrWhiteSpace(templateBase64))
        {
            throw new ValidationException(
                "Nenhuma digital cadastrada para este dedo.",
                new Dictionary<string, string[]> { { "Finger", new[] { "Cadastre a digital antes de executar o teste." } } });
        }

        var hasPendingTask = await _taskRepository.AsQueryable()
            .AnyAsync(task =>
                task.EmployeeId == request.EmployeeId &&
                task.Finger == request.Finger &&
                (task.Status == BiometricTaskStatus.Pending || task.Status == BiometricTaskStatus.InProgress),
                cancellationToken);

        if (hasPendingTask)
        {
            throw new ValidationException(
                "Ja existe uma solicitacao em andamento para este dedo.",
                new Dictionary<string, string[]> { { "Finger", new[] { "Aguarde a conclusao da solicitacao atual." } } });
        }

        var payload = JsonSerializer.Serialize(new
        {
            colaboradorId = employee.RegistrationNumber ?? employee.Id.ToString(),
            nome = employee.Name,
            operation = "verify",
            templateBase64
        });

        var task = new BiometricTask
        {
            BiometricAgentId = agent.Id,
            EmployeeId = employee.Id,
            Finger = request.Finger,
            Status = BiometricTaskStatus.Pending,
            EmployeeName = employee.Name,
            EmployeeRegistrationNumber = employee.RegistrationNumber,
            RequestedByUserId = _currentUserService.GetUserId(),
            RequestedByUserName = _currentUserService.GetUserName(),
            PayloadJson = payload,
            TenantId = employee.TenantId
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Id;
    }

    public async Task<BiometricTaskPayloadDto?> DequeueTaskAsync(Guid apiKey, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.AsQueryable()
            .FirstOrDefaultAsync(a => a.ApiKey == apiKey, cancellationToken);

        if (agent is null)
        {
            throw new NotFoundException("Agente nÃƒÂ£o encontrado.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        agent.LastSeenAtUtc = utcNow;

        await CancelExpiredTasksAsync(agent.Id, utcNow, cancellationToken);

        if (!agent.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        var task = await _taskRepository.AsQueryable()
            .Where(t => t.BiometricAgentId == agent.Id && t.Status == BiometricTaskStatus.Pending)
            .OrderBy(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        task.Status = BiometricTaskStatus.InProgress;
        task.AssignedAtUtc = utcNow;
        task.StartedAtUtc = utcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BiometricTaskPayloadDto(
            task.Id,
            task.EmployeeId,
            task.EmployeeName,
            task.EmployeeRegistrationNumber,
            task.Finger,
            task.PayloadJson ?? string.Empty);
    }

    public async Task ClearEnrollmentsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException("Colaborador não encontrado.");
        }

        var tasks = await _taskRepository.AsQueryable()
            .Where(task => task.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
        {
            return;
        }

        await _taskRepository.RemoveRangeAsync(tasks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<BiometricTaskStatusDto?> GetTaskStatusAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException(
                "N\u00E3o foi poss\u00EDvel identificar a empresa.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "A sess\u00E3o expirou. Fa\u00E7a login novamente." } } });
        }

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null || task.TenantId != tenantId)
        {
            return null;
        }

        var operation = GetTaskOperation(task.PayloadJson) ?? string.Empty;

        return new BiometricTaskStatusDto(
            task.Id,
            task.EmployeeId,
            task.Status,
            operation,
            task.FailureReason,
            task.CompletedAtUtc);
    }

    private async Task CancelExpiredTasksAsync(Guid agentId, DateTime referenceUtc, CancellationToken cancellationToken)
    {
        var timeoutThreshold = referenceUtc - CaptureTimeout;

        var expiredTasks = await _taskRepository.AsQueryable()
            .Where(t => t.BiometricAgentId == agentId
                        && t.Status == BiometricTaskStatus.InProgress
                        && t.StartedAtUtc.HasValue
                        && t.StartedAtUtc <= timeoutThreshold)
            .ToListAsync(cancellationToken);

        if (expiredTasks.Count == 0)
        {
            return;
        }

        foreach (var task in expiredTasks)
        {
            task.Status = BiometricTaskStatus.Cancelled;
            task.CompletedAtUtc = referenceUtc;
            task.CompletedByUserId = null;
            task.CompletedByUserName = null;
            task.FailureReason = "Cancelado automaticamente apos 5 minutos sem resposta do agente.";
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}






