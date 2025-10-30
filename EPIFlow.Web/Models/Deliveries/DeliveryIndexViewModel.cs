using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Models.Deliveries;

public class DeliveryIndexViewModel
{
    public Guid? SelectedEmployeeId { get; set; }
    public Guid? SelectedEpiTypeId { get; set; }
    public IEnumerable<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> EpiTypes { get; set; } = new List<SelectListItem>();
    public IReadOnlyCollection<DeliveryListItemViewModel> Deliveries { get; set; } = new List<DeliveryListItemViewModel>();
}
