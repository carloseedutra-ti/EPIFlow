using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Users.DTOs;

public record UserCreateDto(
    string Email,
    string Password,
    string? FullName,
    string? Department,
    string? JobTitle,
    Guid? DefaultBiometricAgentId,
    IReadOnlyCollection<string> Roles,
    bool IsActive);
