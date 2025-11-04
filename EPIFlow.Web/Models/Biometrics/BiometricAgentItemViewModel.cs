using System;

namespace EPIFlow.Web.Models.Biometrics;

public class BiometricAgentItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MachineName { get; set; }
    public bool IsActive { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public Guid ApiKey { get; set; }
    public int PollingIntervalSeconds { get; set; }
}
