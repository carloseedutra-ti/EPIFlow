using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Biometrics.DTOs;

public record BiometricTaskPayloadDto(
    Guid TaskId,
    Guid EmployeeId,
    string EmployeeName,
    string? EmployeeRegistrationNumber,
    FingerType Finger,
    string PayloadJson);
