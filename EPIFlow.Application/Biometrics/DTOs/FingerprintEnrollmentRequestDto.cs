using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Biometrics.DTOs;

public record FingerprintEnrollmentRequestDto(
    Guid EmployeeId,
    Guid AgentId,
    FingerType Finger);
