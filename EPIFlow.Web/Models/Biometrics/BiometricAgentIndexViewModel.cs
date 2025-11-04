using System.Collections.Generic;

namespace EPIFlow.Web.Models.Biometrics;

public class BiometricAgentIndexViewModel
{
    public IList<BiometricAgentItemViewModel> Agents { get; set; } = new List<BiometricAgentItemViewModel>();

    public BiometricAgentFormViewModel Form { get; set; } = new();
}
