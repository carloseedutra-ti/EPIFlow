using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Models.Reports;

public class ReportsViewModel
{
    public IEnumerable<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
    public Guid? SelectedEmployeeId { get; set; }
    public DateTime ReferenceDate { get; set; } = DateTime.Today;
    public int ThresholdInDays { get; set; } = 30;
}
