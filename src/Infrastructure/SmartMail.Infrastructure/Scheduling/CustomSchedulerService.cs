using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartMail.Application.Common.Interfaces;
using System.Collections.Concurrent;

namespace SmartMail.Infrastructure.Scheduling;

public class ScheduledJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string JobType { get; set; } = string.Empty; // Email, Campaign, Batch
    public Guid EntityId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public bool Executed { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomSchedulerService : BackgroundService, ISchedulerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomSchedulerService> _logger;
    private readonly ConcurrentDictionary<string, ScheduledJob> _scheduledJobs;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10); // Check every 10 seconds

    public CustomSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<CustomSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _scheduledJobs = new ConcurrentDictionary<string, ScheduledJob>();
    }

    public Task ScheduleEmailAsync(Guid emailId, DateTime scheduledTime, CancellationToken cancellationToken = default)
    {
        var job = new ScheduledJob
        {
            JobType = "Email",
            EntityId = emailId,
            ScheduledTime = scheduledTime,
            Status = "Scheduled"
        };

        _scheduledJobs.TryAdd(job.Id, job);
        _logger.LogInformation("Scheduled email {EmailId} for {ScheduledTime}", emailId, scheduledTime);

        return Task.CompletedTask;
    }

    public Task ScheduleCampaignAsync(Guid campaignId, DateTime scheduledTime, CancellationToken cancellationToken = default)
    {
        var job = new ScheduledJob
        {
            JobType = "Campaign",
            EntityId = campaignId,
            ScheduledTime = scheduledTime,
            Status = "Scheduled"
        };

        _scheduledJobs.TryAdd(job.Id, job);
        _logger.LogInformation("Scheduled campaign {CampaignId} for {ScheduledTime}", campaignId, scheduledTime);

        return Task.CompletedTask;
    }

    public Task ScheduleBatchAsync(Guid batchId, DateTime scheduledTime, CancellationToken cancellationToken = default)
    {
        var job = new ScheduledJob
        {
            JobType = "Batch",
            EntityId = batchId,
            ScheduledTime = scheduledTime,
            Status = "Scheduled"
        };

        _scheduledJobs.TryAdd(job.Id, job);
        _logger.LogInformation("Scheduled batch {BatchId} for {ScheduledTime}", batchId, scheduledTime);

        return Task.CompletedTask;
    }

    public Task CancelScheduledJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (_scheduledJobs.TryRemove(jobId, out var job))
        {
            _logger.LogInformation("Cancelled scheduled job {JobId}", jobId);
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (_scheduledJobs.TryGetValue(jobId, out var job))
        {
            return Task.FromResult<string?>(job.Status);
        }

        return Task.FromResult<string?>(null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Custom Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledJobs(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled jobs");
            }
        }

        _logger.LogInformation("Custom Scheduler Service stopped");
    }

    private async Task ProcessScheduledJobs(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var jobsToExecute = _scheduledJobs.Values
            .Where(j => !j.Executed && j.ScheduledTime <= now)
            .ToList();

        foreach (var job in jobsToExecute)
        {
            try
            {
                job.Status = "Executing";

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                switch (job.JobType)
                {
                    case "Email":
                        await ExecuteEmailJob(job.EntityId, context, emailService, cancellationToken);
                        break;
                    case "Campaign":
                        await ExecuteCampaignJob(job.EntityId, context, emailService, cancellationToken);
                        break;
                    case "Batch":
                        await ExecuteBatchJob(job.EntityId, context, emailService, cancellationToken);
                        break;
                }

                job.Executed = true;
                job.ExecutedAt = DateTime.UtcNow;
                job.Status = "Completed";

                _logger.LogInformation("Executed scheduled job {JobId} of type {JobType}", job.Id, job.JobType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing job {JobId}", job.Id);
                job.Status = "Failed";
                job.ErrorMessage = ex.Message;
            }
        }

        // Clean up old executed jobs (older than 24 hours)
        var oldJobs = _scheduledJobs.Values
            .Where(j => j.Executed && j.ExecutedAt.HasValue && j.ExecutedAt.Value < DateTime.UtcNow.AddHours(-24))
            .ToList();

        foreach (var oldJob in oldJobs)
        {
            _scheduledJobs.TryRemove(oldJob.Id, out _);
        }
    }

    private async Task ExecuteEmailJob(
        Guid emailId,
        IApplicationDbContext context,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var email = await context.Emails.FindAsync(new object[] { emailId }, cancellationToken);

        if (email == null)
        {
            _logger.LogWarning("Email {EmailId} not found", emailId);
            return;
        }

        await emailService.SendEmailAsync(email, null, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ExecuteCampaignJob(
        Guid campaignId,
        IApplicationDbContext context,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var campaign = await context.EmailCampaigns
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found", campaignId);
            return;
        }

        // Start the campaign
        campaign.Start();

        // Process recipients
        var recipientsToProcess = campaign.Recipients.Where(r => !r.Sent).ToList();

        _logger.LogInformation("Processing campaign {CampaignId} with {RecipientCount} recipients",
            campaignId, recipientsToProcess.Count);

        foreach (var recipient in recipientsToProcess)
        {
            // TODO: Create and send email for each recipient
            // This would involve template processing, personalization, etc.
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ExecuteBatchJob(
        Guid batchId,
        IApplicationDbContext context,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var batch = await context.BatchSchedules
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
        {
            _logger.LogWarning("Batch {BatchId} not found", batchId);
            return;
        }

        // TODO: Process batch emails
        // Similar to campaign processing but for a specific batch

        batch.MarkAsCompleted();
        await context.SaveChangesAsync(cancellationToken);
    }
}
