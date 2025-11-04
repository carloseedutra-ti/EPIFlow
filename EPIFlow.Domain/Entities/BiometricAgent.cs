using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;

namespace EPIFlow.Domain.Entities;

public class BiometricAgent : AuditableEntity
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? MachineName { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }

    public Guid ApiKey { get; set; } = Guid.NewGuid();

    public bool IsActive { get; set; } = true;

    public DateTime? LastSeenAtUtc { get; set; }

    public int PollingIntervalSeconds { get; set; } = 5;

    public ICollection<BiometricTask> Tasks { get; set; } = new List<BiometricTask>();
}
