using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Users.DTOs;

public record UserListItemDto(
    Guid Id,
    string Email,
    string? FullName,
    string? Department,
    string? JobTitle,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    Guid? DefaultBiometricAgentId,
    string? DefaultBiometricAgentName);
