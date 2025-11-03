using SmartMail.Domain.Entities;

namespace SmartMail.Application.Common.Interfaces;

// Repository interfaces for data access using stored procedures
public interface IEmailRepository
{
    Task<Email?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Email?> GetByTrackingIdAsync(string trackingId, CancellationToken cancellationToken = default);
    Task<List<Email>> GetByTenantIdAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(Email email, CancellationToken cancellationToken = default);
    Task UpdateAsync(Email email, CancellationToken cancellationToken = default);
}

public interface IEmailCampaignRepository
{
    Task<EmailCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EmailCampaign>> GetByTenantIdAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(EmailCampaign campaign, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailCampaign campaign, CancellationToken cancellationToken = default);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
}

public interface IEmailClickRepository
{
    Task<Guid> AddAsync(EmailClick emailClick, CancellationToken cancellationToken = default);
    Task<List<EmailClick>> GetByEmailIdAsync(Guid emailId, CancellationToken cancellationToken = default);
    Task<List<EmailClick>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
}

public interface IUnsubscribeRepository
{
    Task<Guid> AddAsync(UnsubscribeRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsUnsubscribedAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);
    Task<List<UnsubscribeRequest>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IBatchScheduleRepository
{
    Task<BatchSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<BatchSchedule>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(BatchSchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(BatchSchedule schedule, CancellationToken cancellationToken = default);
}

public interface ICampaignRecipientRepository
{
    Task<List<CampaignRecipient>> GetByCampaignIdAsync(Guid campaignId, bool sentOnly, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(CampaignRecipient recipient, CancellationToken cancellationToken = default);
    Task UpdateAsync(CampaignRecipient recipient, CancellationToken cancellationToken = default);
}
