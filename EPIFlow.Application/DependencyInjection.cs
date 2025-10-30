using EPIFlow.Application.Deliveries.Services;
using EPIFlow.Application.Dashboard.Services;
using EPIFlow.Application.EpiTypes.Services;
using EPIFlow.Application.Inventory.Services;
using EPIFlow.Application.Reports.Services;
using EPIFlow.Application.Employees.Services;
using EPIFlow.Application.Tenants.Services;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace EPIFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEpiTypeService, EpiTypeService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();

        return services;
    }
}
