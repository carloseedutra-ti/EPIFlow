using System;

namespace EPIFlow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? GetUserId();
    string? GetUserName();
    bool IsInRole(string role);
}
