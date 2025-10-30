using System;

namespace EPIFlow.Application.Deliveries.DTOs;

public record DeliveryItemDto(
    Guid Id,
    Guid EpiTypeId,
    string EpiCode,
    string EpiDescription,
    int Quantity,
    DateTime ValidUntil,
    DateTime? ReturnedAtUtc,
    int? ReturnedQuantity);
