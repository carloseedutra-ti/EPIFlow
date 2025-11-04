using System;

namespace EPIFlow.Web.Models.Users;

public class UserListItemViewModel
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public bool IsActive { get; init; }
    public string RolesDescription { get; init; } = string.Empty;
    public string? DefaultAgentName { get; init; }
}
