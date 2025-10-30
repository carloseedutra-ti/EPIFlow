using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Application.EpiTypes.DTOs;

public record EpiTypeDto(
    Guid Id,
    string Code,
    string Description,
    EpiCategory Category,
    int ValidityInMonths,
    string? CaNumber,
    bool IsActive,
    int QuantityAvailable,
    int MinimumQuantity);
