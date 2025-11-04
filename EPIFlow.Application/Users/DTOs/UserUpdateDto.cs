using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Users.DTOs;

public record UserUpdateDto(
    string Email,
    string? FullName,
    string? Department,
    string? JobTitle,
    Guid? DefaultBiometricAgentId,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    string? NewPassword);
