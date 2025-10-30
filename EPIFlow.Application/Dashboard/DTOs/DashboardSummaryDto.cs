namespace EPIFlow.Application.Dashboard.DTOs;

public record DashboardSummaryDto(
    int TotalEpiTypes,
    int TotalActiveEmployees,
    int DeliveriesInCurrentMonth,
    int LowStockItems);
