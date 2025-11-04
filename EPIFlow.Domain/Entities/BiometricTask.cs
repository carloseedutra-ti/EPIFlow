using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;
using EPIFlow.Domain.Enums;

namespace EPIFlow.Domain.Entities;

public class BiometricTask : AuditableEntity
{
    [Required]
    public Guid BiometricAgentId { get; set; }

    public BiometricAgent? BiometricAgent { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public FingerType Finger { get; set; }

    public BiometricTaskStatus Status { get; set; } = BiometricTaskStatus.Pending;

    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EmployeeRegistrationNumber { get; set; }

    [MaxLength(50)]
    public string? RequestedByUserName { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public DateTime? AssignedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(50)]
    public string? CompletedByUserName { get; set; }

    public Guid? CompletedByUserId { get; set; }

    public string? PayloadJson { get; set; }

    public string? ResultJson { get; set; }

    [MaxLength(300)]
    public string? FailureReason { get; set; }
}
