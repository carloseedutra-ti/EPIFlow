using System;
using System.Collections.Generic;

namespace EPIFlow.Web.Models.Biometrics;

public class AgentTaskResponseModel
{
    public Guid TaskId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeRegistrationNumber { get; set; }
    public int Finger { get; set; }
    public string FingerName { get; set; } = string.Empty;
    public Dictionary<string, object?> Payload { get; set; } = new();
}
