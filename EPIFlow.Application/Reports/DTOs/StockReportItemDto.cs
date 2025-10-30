using System;

namespace EPIFlow.Application.Reports.DTOs;

public record StockReportItemDto(
    string Code,
    string Description,
    string Category,
    int QuantityAvailable,
    int MinimumQuantity,
    bool IsBelowMinimum);
