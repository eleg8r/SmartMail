using SmartMail.Domain.ValueObjects;

namespace SmartMail.Application.Common.Interfaces;

public interface ITenantContext
{
    TenantId GetCurrentTenant();
    Task<bool> ValidateTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
