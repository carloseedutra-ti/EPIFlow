using System;
using System.Security.Claims;
using EPIFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EPIFlow.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(identifier, out var guid) ? guid : null;
    }

    public string? GetUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.Identity?.Name ?? user.FindFirst(ClaimTypes.Name)?.Value;
    }

    public bool IsInRole(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(role) ?? false;
    }
}
