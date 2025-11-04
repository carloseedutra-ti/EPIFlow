using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace EPIFlow.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(150)]
    public string? Department { get; set; }

    [MaxLength(150)]
    public string? JobTitle { get; set; }

    public Guid? DefaultBiometricAgentId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
