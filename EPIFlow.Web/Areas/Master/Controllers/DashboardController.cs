using System.Threading.Tasks;
using EPIFlow.Application.Tenants.Services;
using Microsoft.AspNetCore.Mvc;

namespace EPIFlow.Web.Areas.Master.Controllers;

public class DashboardController : MasterControllerBase
{
    private readonly ITenantManagementService _tenantManagementService;

    public DashboardController(ITenantManagementService tenantManagementService)
    {
        _tenantManagementService = tenantManagementService;
    }

    public async Task<IActionResult> Index()
    {
        var dashboard = await _tenantManagementService.GetDashboardAsync();
        return View(dashboard);
    }
}
