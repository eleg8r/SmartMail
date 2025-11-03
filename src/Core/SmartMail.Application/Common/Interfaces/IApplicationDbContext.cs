using Microsoft.EntityFrameworkCore;
using SmartMail.Domain.Entities;

namespace SmartMail.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Email> Emails { get; }
    DbSet<EmailAttachment> EmailAttachments { get; }
    DbSet<EmailCampaign> EmailCampaigns { get; }
    DbSet<CampaignRecipient> CampaignRecipients { get; }
    DbSet<BatchSchedule> BatchSchedules { get; }
    DbSet<CampaignVariant> CampaignVariants { get; }
    DbSet<DripStep> DripSteps { get; }
    DbSet<EmailClick> EmailClicks { get; }
    DbSet<UnsubscribeRequest> UnsubscribeRequests { get; }
    DbSet<Tenant> Tenants { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
