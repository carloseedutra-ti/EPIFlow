using System;
using System.Collections.Generic;

namespace EPIFlow.Web.Models.Deliveries;

public record DeliveryListItemViewModel(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTime DeliveryDate,
    string ResponsibleName,
    string? DeliveryNumber,
    string? Notes,
    IReadOnlyCollection<DeliveryItemViewModel> Items);
