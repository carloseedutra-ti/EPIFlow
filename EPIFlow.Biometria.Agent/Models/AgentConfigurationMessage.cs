namespace EPIFlow.Biometria.Agent.Models
{
    public class AgentConfigurationMessage
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AgentName { get; set; }
        public int PollingIntervalSeconds { get; set; }
    }
}
