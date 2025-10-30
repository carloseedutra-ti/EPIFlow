using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Dashboard.DTOs;

namespace EPIFlow.Application.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
