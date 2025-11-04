namespace EPIFlow.Web.Models.Biometrics;

public class AgentConfigurationResponseModel
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AgentName { get; set; }
    public int PollingIntervalSeconds { get; set; }
}
