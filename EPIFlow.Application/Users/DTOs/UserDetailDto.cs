using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Users.DTOs;

public record UserDetailDto(
    Guid Id,
    string Email,
    string? FullName,
    string? Department,
    string? JobTitle,
    Guid? DefaultBiometricAgentId,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
