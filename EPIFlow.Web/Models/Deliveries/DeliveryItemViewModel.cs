using System;

namespace EPIFlow.Web.Models.Deliveries;

public record DeliveryItemViewModel(
    Guid Id,
    string EpiCode,
    string EpiDescription,
    int Quantity,
    DateTime ValidUntil,
    int ReturnedQuantity,
    DateTime? ReturnedAtUtc);
