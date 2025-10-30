using System;

namespace EPIFlow.Web.Models.Inventory;

public record InventoryListItemViewModel(
    Guid Id,
    Guid EpiTypeId,
    string EpiCode,
    string EpiDescription,
    int QuantityAvailable,
    int MinimumQuantity,
    string? Location,
    DateTime? LastInventoryDate,
    bool IsBelowMinimum);
