using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Reports.DTOs;

namespace EPIFlow.Application.Reports.Services;

public interface IReportService
{
    Task<IReadOnlyList<StockReportItemDto>> GetStockReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliveryReportItemDto>> GetDeliveryReportByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpiredEpiReportItemDto>> GetExpiredEpiReportAsync(DateTime referenceDate, int thresholdInDays, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateStockReportPdfAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateDeliveryReportPdfAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExpiredEpiReportPdfAsync(DateTime referenceDate, int thresholdInDays, CancellationToken cancellationToken = default);
}
