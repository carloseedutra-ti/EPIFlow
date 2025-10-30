using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Dashboard.DTOs;
using EPIFlow.Domain.Entities;
using EPIFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<EpiType> _epiTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EpiDelivery> _deliveryRepository;
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DashboardService(
        IRepository<EpiType> epiTypeRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EpiDelivery> deliveryRepository,
        IRepository<InventoryItem> inventoryRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _epiTypeRepository = epiTypeRepository;
        _employeeRepository = employeeRepository;
        _deliveryRepository = deliveryRepository;
        _inventoryRepository = inventoryRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var totalEpiTypes = await _epiTypeRepository.AsQueryable()
            .Where(epi => epi.IsActive)
            .CountAsync(cancellationToken);

        var totalActiveEmployees = await _employeeRepository.AsQueryable()
            .Where(employee => employee.Status == EmployeeStatus.Active)
            .CountAsync(cancellationToken);

        var deliveriesInMonth = await _deliveryRepository.AsQueryable()
            .Where(delivery => delivery.DeliveryDate >= startOfMonth)
            .CountAsync(cancellationToken);

        var lowStock = await _inventoryRepository.AsQueryable()
            .Where(item => item.QuantityAvailable <= item.MinimumQuantity)
            .CountAsync(cancellationToken);

        return new DashboardSummaryDto(
            totalEpiTypes,
            totalActiveEmployees,
            deliveriesInMonth,
            lowStock);
    }
}
