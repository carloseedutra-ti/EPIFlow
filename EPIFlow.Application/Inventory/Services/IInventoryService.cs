using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Inventory.DTOs;
using EPIFlow.Application.Inventory.DTOs.Movements;

namespace EPIFlow.Application.Inventory.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetAllAsync(string? searchTerm, CancellationToken cancellationToken = default);
    Task<InventoryItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> RegisterMovementAsync(StockMovementCreateDto dto, CancellationToken cancellationToken = default);
    Task RecalculateStockAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);
}
