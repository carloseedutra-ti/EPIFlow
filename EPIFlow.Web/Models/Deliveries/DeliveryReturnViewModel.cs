using System;
using System.Collections.Generic;

namespace EPIFlow.Web.Models.Deliveries;

public class DeliveryReturnViewModel
{
    public Guid DeliveryId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<DeliveryReturnItemViewModel> Items { get; set; } = new();
}
