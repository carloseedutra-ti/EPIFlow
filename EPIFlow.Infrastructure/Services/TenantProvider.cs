using System;
using EPIFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EPIFlow.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private const string TenantClaimType = "tenant_id";
    private const string TenantHeaderName = "X-Tenant";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue("TenantId", out var tenantFromItems))
        {
            if (tenantFromItems is Guid tenantGuid)
            {
                return tenantGuid;
            }

            if (tenantFromItems is string tenantString && Guid.TryParse(tenantString, out var parsedTenant))
            {
                return parsedTenant;
            }
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var claimValue = user.FindFirst(TenantClaimType)?.Value ?? user.FindFirst("tenant")?.Value;
            if (Guid.TryParse(claimValue, out var tenantIdFromClaim))
            {
                return tenantIdFromClaim;
            }
        }

        if (httpContext.Request.Headers.TryGetValue(TenantHeaderName, out var tenantHeader) && Guid.TryParse(tenantHeader, out var tenantIdFromHeader))
        {
            return tenantIdFromHeader;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Identifier", out var tenantIdentifierHeader))
        {
            return ResolveTenantByIdentifier(tenantIdentifierHeader!);
        }

        return null;
    }

    public string? GetTenantIdentifier()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue("TenantIdentifier", out var tenantIdentifier) && tenantIdentifier is string identifier)
        {
            return identifier;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Identifier", out var headerIdentifier))
        {
            return headerIdentifier.ToString();
        }

        var host = httpContext.Request.Host.Host;
        if (!string.IsNullOrWhiteSpace(host))
        {
            var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 2)
            {
                return segments[0];
            }
        }

        return null;
    }

    private Guid? ResolveTenantByIdentifier(string identifier)
    {
        if (Guid.TryParse(identifier, out var parsedGuid))
        {
            return parsedGuid;
        }

        return null;
    }
}
