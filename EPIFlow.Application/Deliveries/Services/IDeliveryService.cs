using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Deliveries.DTOs;
using EPIFlow.Application.Deliveries.DTOs.Requests;

namespace EPIFlow.Application.Deliveries.Services;

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryDto>> GetAllAsync(Guid? employeeId, Guid? epiTypeId, CancellationToken cancellationToken = default);
    Task<DeliveryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> RegisterDeliveryAsync(DeliveryCreateDto dto, CancellationToken cancellationToken = default);
    Task ReturnItemsAsync(IEnumerable<DeliveryReturnDto> items, CancellationToken cancellationToken = default);
}
