using System;

namespace EPIFlow.Application.Common.Interfaces;

public interface ITenantProvider
{
    Guid? GetTenantId();
    string? GetTenantIdentifier();
}
