using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EPIFlow.Web.Middleware;

public class TenantResolutionMiddleware
{
    private const string TenantIdKey = "TenantId";
    private const string TenantIdentifierKey = "TenantIdentifier";
    private const string TenantHeader = "X-Tenant";
    private const string TenantIdentifierHeader = "X-Tenant-Identifier";
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            ResolveTenantId(context);
            ResolveTenantIdentifier(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while resolving tenant information.");
        }

        await _next(context);
    }

    private static void ResolveTenantId(HttpContext context)
    {
        if (context.Items.ContainsKey(TenantIdKey))
        {
            return;
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value ?? context.User.FindFirst("tenant")?.Value;
            if (Guid.TryParse(tenantClaim, out var tenantFromClaim))
            {
                context.Items[TenantIdKey] = tenantFromClaim;
                return;
            }
        }

        var queryTenant = context.Request.Query["tenantId"];
        if (!string.IsNullOrWhiteSpace(queryTenant) && Guid.TryParse(queryTenant, out var tenantFromQuery))
        {
            context.Items[TenantIdKey] = tenantFromQuery;
            return;
        }

        if (context.Request.Headers.TryGetValue(TenantHeader, out var tenantHeader) &&
            Guid.TryParse(tenantHeader, out var tenantFromHeader))
        {
            context.Items[TenantIdKey] = tenantFromHeader;
        }
    }

    private static void ResolveTenantIdentifier(HttpContext context)
    {
        if (context.Items.ContainsKey(TenantIdentifierKey))
        {
            return;
        }

        if (context.Request.Headers.TryGetValue(TenantIdentifierHeader, out var identifierHeader) &&
            !string.IsNullOrWhiteSpace(identifierHeader))
        {
            context.Items[TenantIdentifierKey] = identifierHeader.ToString();
            return;
        }

        var host = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return;
        }

        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 2)
        {
            context.Items[TenantIdentifierKey] = segments[0];
        }
    }
}
