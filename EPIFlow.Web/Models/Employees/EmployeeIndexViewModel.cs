using System.Collections.Generic;

namespace EPIFlow.Web.Models.Employees;

public class EmployeeIndexViewModel
{
    public string? SearchTerm { get; set; }
    public IReadOnlyCollection<EmployeeListItemViewModel> Employees { get; set; } = new List<EmployeeListItemViewModel>();
}
