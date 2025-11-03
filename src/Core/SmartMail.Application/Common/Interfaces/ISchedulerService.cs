namespace SmartMail.Application.Common.Interfaces;

public interface ISchedulerService
{
    Task ScheduleEmailAsync(
        Guid emailId,
        DateTime scheduledTime,
        CancellationToken cancellationToken = default);

    Task ScheduleCampaignAsync(
        Guid campaignId,
        DateTime scheduledTime,
        CancellationToken cancellationToken = default);

    Task ScheduleBatchAsync(
        Guid batchId,
        DateTime scheduledTime,
        CancellationToken cancellationToken = default);

    Task CancelScheduledJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    Task<string?> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}

public enum SchedulerType
{
    Custom = 0,
    Hangfire = 1,
    Quartz = 2,
    RabbitMq = 3
}
