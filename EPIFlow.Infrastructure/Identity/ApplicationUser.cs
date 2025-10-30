using System;
using Microsoft.AspNetCore.Identity;

namespace EPIFlow.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
