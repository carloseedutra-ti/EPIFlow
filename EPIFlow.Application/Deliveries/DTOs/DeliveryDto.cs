using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Deliveries.DTOs;

public record DeliveryDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTime DeliveryDate,
    Guid ResponsibleUserId,
    string ResponsibleName,
    string? DeliveryNumber,
    string? Notes,
    IReadOnlyCollection<DeliveryItemDto> Items);
