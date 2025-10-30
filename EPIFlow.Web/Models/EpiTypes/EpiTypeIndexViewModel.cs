using System.Collections.Generic;

namespace EPIFlow.Web.Models.EpiTypes;

public class EpiTypeIndexViewModel
{
    public string? SearchTerm { get; set; }
    public IReadOnlyCollection<EpiTypeListItemViewModel> EpiTypes { get; set; } = new List<EpiTypeListItemViewModel>();
}
