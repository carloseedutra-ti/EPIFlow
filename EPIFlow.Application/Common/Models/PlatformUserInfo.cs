using System;

namespace EPIFlow.Application.Common.Models;

public record PlatformUserInfo(
    Guid Id,
    string Email,
    string? FullName,
    bool IsActive);
