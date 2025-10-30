using System;
using System.Linq;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Employees.Services;
using EPIFlow.Application.Reports.Services;
using EPIFlow.Domain.Constants;
using EPIFlow.Domain.Enums;
using EPIFlow.Web.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Controllers;

[Authorize(Roles = SystemRoles.Administrator + "," + SystemRoles.Warehouse)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IEmployeeService _employeeService;

    public ReportsController(IReportService reportService, IEmployeeService employeeService)
    {
        _reportService = reportService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildReportsViewModelAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadStockReport()
    {
        var pdfBytes = await _reportService.GenerateStockReportPdfAsync();
        return File(pdfBytes, "application/pdf", $"Relatorio_Estoque_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadDeliveriesReport(Guid? employeeId)
    {
        if (!employeeId.HasValue)
        {
            TempData["Error"] = "Selecione o colaborador para gerar o relatório.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var pdfBytes = await _reportService.GenerateDeliveryReportPdfAsync(employeeId.Value);
            return File(pdfBytes, "application/pdf", $"Relatorio_Entregas_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadExpiredReport(DateTime referenceDate, int thresholdInDays)
    {
        if (thresholdInDays < 0)
        {
            TempData["Error"] = "Informe um prazo válido.";
            return RedirectToAction(nameof(Index));
        }

        var pdfBytes = await _reportService.GenerateExpiredEpiReportPdfAsync(referenceDate, thresholdInDays);
        return File(pdfBytes, "application/pdf", $"Relatorio_EPIs_Vencidos_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    private async Task<ReportsViewModel> BuildReportsViewModelAsync()
    {
        var employees = await _employeeService.GetAllAsync(null);
        return new ReportsViewModel
        {
            Employees = employees
                .Where(employee => employee.Status == EmployeeStatus.Active)
                .OrderBy(employee => employee.Name)
                .Select(employee => new SelectListItem(employee.Name, employee.Id.ToString()))
                .ToList(),
            ReferenceDate = DateTime.Today,
            ThresholdInDays = 30
        };
    }
}
