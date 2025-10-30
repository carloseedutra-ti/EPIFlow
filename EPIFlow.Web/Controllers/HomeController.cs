using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EPIFlow.Web.Models;

namespace EPIFlow.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var tenantClaim = User?.FindFirst("tenant_id")?.Value ?? User?.FindFirst("tenant")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId) && tenantId == Guid.Empty)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Master" });
        }

        return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
