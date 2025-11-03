using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMail.Application.Common.Interfaces;

namespace SmartMail.Application.Features.Campaigns.Commands;

public record ScheduleCampaignCommand : IRequest<bool>
{
    public Guid CampaignId { get; init; }
}

public class ScheduleCampaignCommandHandler : IRequestHandler<ScheduleCampaignCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ISchedulerService _schedulerService;
    private readonly ILogger<ScheduleCampaignCommandHandler> _logger;

    public ScheduleCampaignCommandHandler(
        IApplicationDbContext context,
        ISchedulerService schedulerService,
        ILogger<ScheduleCampaignCommandHandler> logger)
    {
        _context = context;
        _schedulerService = schedulerService;
        _logger = logger;
    }

    public async Task<bool> Handle(ScheduleCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _context.EmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign == null)
            throw new Exception($"Campaign {request.CampaignId} not found");

        campaign.Schedule();
        await _context.SaveChangesAsync(cancellationToken);

        // Schedule the campaign with the scheduler
        if (campaign.ScheduledStartDate.HasValue)
        {
            await _schedulerService.ScheduleCampaignAsync(
                campaign.Id,
                campaign.ScheduledStartDate.Value,
                cancellationToken);
        }

        _logger.LogInformation("Scheduled campaign {CampaignId}", request.CampaignId);

        return true;
    }
}

public record StartCampaignCommand : IRequest<bool>
{
    public Guid CampaignId { get; init; }
}

public class StartCampaignCommandHandler : IRequestHandler<StartCampaignCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<StartCampaignCommandHandler> _logger;

    public StartCampaignCommandHandler(
        IApplicationDbContext context,
        ILogger<StartCampaignCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(StartCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _context.EmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign == null)
            throw new Exception($"Campaign {request.CampaignId} not found");

        campaign.Start();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Started campaign {CampaignId}", request.CampaignId);

        return true;
    }
}

public record PauseCampaignCommand : IRequest<bool>
{
    public Guid CampaignId { get; init; }
}

public class PauseCampaignCommandHandler : IRequestHandler<PauseCampaignCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PauseCampaignCommandHandler> _logger;

    public PauseCampaignCommandHandler(
        IApplicationDbContext context,
        ILogger<PauseCampaignCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(PauseCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _context.EmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign == null)
            throw new Exception($"Campaign {request.CampaignId} not found");

        campaign.Pause();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Paused campaign {CampaignId}", request.CampaignId);

        return true;
    }
}

public record ResumeCampaignCommand : IRequest<bool>
{
    public Guid CampaignId { get; init; }
}

public class ResumeCampaignCommandHandler : IRequestHandler<ResumeCampaignCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResumeCampaignCommandHandler> _logger;

    public ResumeCampaignCommandHandler(
        IApplicationDbContext context,
        ILogger<ResumeCampaignCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ResumeCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _context.EmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign == null)
            throw new Exception($"Campaign {request.CampaignId} not found");

        campaign.Resume();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resumed campaign {CampaignId}", request.CampaignId);

        return true;
    }
}

public record CancelCampaignCommand : IRequest<bool>
{
    public Guid CampaignId { get; init; }
}

public class CancelCampaignCommandHandler : IRequestHandler<CancelCampaignCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CancelCampaignCommandHandler> _logger;

    public CancelCampaignCommandHandler(
        IApplicationDbContext context,
        ILogger<CancelCampaignCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _context.EmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign == null)
            throw new Exception($"Campaign {request.CampaignId} not found");

        campaign.Cancel();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled campaign {CampaignId}", request.CampaignId);

        return true;
    }
}
