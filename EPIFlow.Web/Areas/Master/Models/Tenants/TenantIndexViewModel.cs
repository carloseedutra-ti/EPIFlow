using System.Collections.Generic;
using EPIFlow.Application.Tenants.DTOs;

namespace EPIFlow.Web.Areas.Master.Models.Tenants;

public class TenantIndexViewModel
{
    public string? SearchTerm { get; set; }
    public IReadOnlyCollection<TenantListItemDto> Tenants { get; set; } = new List<TenantListItemDto>();
}
