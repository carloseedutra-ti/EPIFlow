using System;
using System.Collections.Generic;
using EPIFlow.Application.Inventory.DTOs.Movements;

namespace EPIFlow.Application.Inventory.DTOs;

public record InventoryItemDto(
    Guid Id,
    Guid EpiTypeId,
    string EpiCode,
    string EpiDescription,
    int QuantityAvailable,
    int MinimumQuantity,
    string? Location,
    DateTime? LastInventoryDate,
    IReadOnlyCollection<StockMovementDto> RecentMovements);
