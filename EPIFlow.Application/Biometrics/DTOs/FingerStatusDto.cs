using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Biometrics.DTOs;

public record FingerStatusDto(
    FingerType Finger,
    string DisplayName,
    BiometricTaskStatus? Status,
    string StatusLabel,
    DateTime? RequestedAtUtc,
    DateTime? UpdatedAtUtc,
    Guid? TaskId,
    bool HasTemplate);
