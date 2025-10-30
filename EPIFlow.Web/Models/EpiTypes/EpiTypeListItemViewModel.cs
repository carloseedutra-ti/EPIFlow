using System;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Web.Models.EpiTypes;

public record EpiTypeListItemViewModel(
    Guid Id,
    string Code,
    string Description,
    EpiCategory Category,
    int ValidityInMonths,
    string? CaNumber,
    bool IsActive,
    int QuantityAvailable,
    int MinimumQuantity);
