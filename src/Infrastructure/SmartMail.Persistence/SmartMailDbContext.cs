using Microsoft.EntityFrameworkCore;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Common;
using SmartMail.Domain.Entities;
using System.Reflection;

namespace SmartMail.Persistence;

public class SmartMailDbContext : DbContext, IApplicationDbContext
{
    public SmartMailDbContext(DbContextOptions<SmartMailDbContext> options)
        : base(options)
    {
    }

    public DbSet<Email> Emails => Set<Email>();
    public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
    public DbSet<EmailCampaign> EmailCampaigns => Set<EmailCampaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<BatchSchedule> BatchSchedules => Set<BatchSchedule>();
    public DbSet<CampaignVariant> CampaignVariants => Set<CampaignVariant>();
    public DbSet<DripStep> DripSteps => Set<DripStep>();
    public DbSet<EmailClick> EmailClicks => Set<EmailClick>();
    public DbSet<UnsubscribeRequest> UnsubscribeRequests => Set<UnsubscribeRequest>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        var domainEvents = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Clear domain events after saving
        foreach (var entity in ChangeTracker.Entries<AggregateRoot<Guid>>().Select(e => e.Entity))
        {
            entity.ClearDomainEvents();
        }

        // TODO: Publish domain events to event bus
        // This would integrate with MediatR or other event dispatcher

        return result;
    }
}
