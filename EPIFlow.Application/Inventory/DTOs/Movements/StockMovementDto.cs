using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.Inventory.DTOs.Movements;

public record StockMovementDto(
    Guid Id,
    StockMovementType MovementType,
    int Quantity,
    DateTime MovementDate,
    string? Reference,
    string? Notes);
