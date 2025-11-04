using System;
using System.Collections.Generic;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Web.Models.Biometrics;

public class EmployeeBiometricOverviewViewModel
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public IList<BiometricAgentViewModel> Agents { get; set; } = new List<BiometricAgentViewModel>();
    public IList<FingerStatusViewModel> Fingers { get; set; } = new List<FingerStatusViewModel>();
}

public class BiometricAgentViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public int PollingIntervalSeconds { get; set; }
}

public class FingerStatusViewModel
{
    public int FingerValue { get; set; }
    public string FingerKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? StatusValue { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime? RequestedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? TaskId { get; set; }
    public bool CanTest { get; set; }
}
