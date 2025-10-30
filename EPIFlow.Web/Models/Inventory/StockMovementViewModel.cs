using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Web.Models.Inventory;

public record StockMovementViewModel(
    Guid Id,
    StockMovementType MovementType,
    int Quantity,
    DateTime MovementDate,
    string? Reference,
    string? Notes);
