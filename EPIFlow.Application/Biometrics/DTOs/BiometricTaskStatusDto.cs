using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Biometrics.DTOs;

public record BiometricTaskStatusDto(
    Guid TaskId,
    Guid EmployeeId,
    BiometricTaskStatus Status,
    string Operation,
    string? FailureReason,
    DateTime? CompletedAtUtc);
