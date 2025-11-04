using System;

namespace EPIFlow.Application.Biometrics.DTOs;

public record BiometricAgentDto(
    Guid Id,
    string Name,
    string? Description,
    string? MachineName,
    bool IsActive,
    bool IsOnline,
    DateTime? LastSeenAtUtc,
    int PollingIntervalSeconds,
    Guid ApiKey);
