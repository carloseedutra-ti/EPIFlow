using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Biometrics.DTOs;

public record FingerprintVerificationRequestDto(
    Guid EmployeeId,
    Guid AgentId,
    FingerType Finger);
