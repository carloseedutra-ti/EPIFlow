using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Biometrics.DTOs;

public record EmployeeBiometricOverviewDto(
    Guid EmployeeId,
    string EmployeeName,
    IReadOnlyList<FingerStatusDto> Fingers,
    IReadOnlyList<BiometricAgentDto> Agents);
