using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EPIFlow.Web.Areas.Master.Controllers;

[Area("Master")]
[Authorize]
public abstract class MasterControllerBase : Controller
{
    protected bool IsPlatformAdmin => GetTenantId() == Guid.Empty;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!IsPlatformAdmin)
        {
            context.Result = Forbid();
        }

        base.OnActionExecuting(context);
    }

    private Guid? GetTenantId()
    {
        var claim = User?.FindFirst("tenant_id")?.Value ?? User?.FindFirst("tenant")?.Value;
        return Guid.TryParse(claim, out var tenantId) ? tenantId : null;
    }
}
