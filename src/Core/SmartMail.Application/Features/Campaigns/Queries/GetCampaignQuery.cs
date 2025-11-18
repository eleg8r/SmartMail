using MediatR;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Application.DTOs;

namespace SmartMail.Application.Features.Campaigns.Queries;

public record GetCampaignQuery : IRequest<CampaignDto?>
{
    public Guid CampaignId { get; init; }
}

public class GetCampaignQueryHandler : IRequestHandler<GetCampaignQuery, CampaignDto?>
{
    private readonly IEmailCampaignRepository _campaignRepository;

    public GetCampaignQueryHandler(IEmailCampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public async Task<CampaignDto?> Handle(GetCampaignQuery request, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);

        if (campaign == null)
            return null;

        return new CampaignDto
        {
            Id = campaign.Id,
            TenantId = campaign.TenantId.Value,
            Name = campaign.Name,
            Description = campaign.Description,
            Type = campaign.Type,
            Status = campaign.Status,
            From = campaign.FromAddress.Address,
            Subject = campaign.Subject,
            HtmlTemplate = campaign.HtmlTemplate,
            TextTemplate = campaign.TextTemplate,
            TotalRecipients = campaign.TotalRecipients,
            EmailsSent = campaign.EmailsSent,
            EmailsDelivered = campaign.EmailsDelivered,
            EmailsOpened = campaign.EmailsOpened,
            EmailsClicked = campaign.EmailsClicked,
            EmailsBounced = campaign.EmailsBounced,
            EmailsFailed = campaign.EmailsFailed,
            Unsubscribes = campaign.Unsubscribes,
            SpamComplaints = campaign.SpamComplaints,
            ScheduledStartDate = campaign.ScheduledStartDate,
            ScheduledEndDate = campaign.ScheduledEndDate,
            TimeZone = campaign.TimeZone,
            BatchSize = campaign.BatchSize,
            MaxEmailsPerHour = campaign.MaxEmailsPerHour,
            MaxEmailsPerDay = campaign.MaxEmailsPerDay,
            IsAbTest = campaign.IsAbTest,
            IsDripCampaign = campaign.IsDripCampaign,
            IncludeUnsubscribeLink = campaign.IncludeUnsubscribeLink,
            EnableOpenTracking = campaign.EnableOpenTracking,
            EnableClickTracking = campaign.EnableClickTracking,
            CreatedAt = campaign.CreatedAt,
            StartedAt = campaign.StartedAt,
            CompletedAt = campaign.CompletedAt
        };
    }
}

public record GetCampaignsQuery : IRequest<List<CampaignDto>>
{
    public Guid TenantId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class GetCampaignsQueryHandler : IRequestHandler<GetCampaignsQuery, List<CampaignDto>>
{
    private readonly IEmailCampaignRepository _campaignRepository;

    public GetCampaignsQueryHandler(IEmailCampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public async Task<List<CampaignDto>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.PageNumber - 1) * request.PageSize;
        var campaigns = await _campaignRepository.GetByTenantIdAsync(
            request.TenantId,
            skip,
            request.PageSize,
            cancellationToken);

        return campaigns.Select(campaign => new CampaignDto
        {
            Id = campaign.Id,
            TenantId = campaign.TenantId.Value,
            Name = campaign.Name,
            Description = campaign.Description,
            Type = campaign.Type,
            Status = campaign.Status,
            From = campaign.FromAddress.Address,
            Subject = campaign.Subject,
            HtmlTemplate = campaign.HtmlTemplate,
            TextTemplate = campaign.TextTemplate,
            TotalRecipients = campaign.TotalRecipients,
            EmailsSent = campaign.EmailsSent,
            EmailsDelivered = campaign.EmailsDelivered,
            EmailsOpened = campaign.EmailsOpened,
            EmailsClicked = campaign.EmailsClicked,
            EmailsBounced = campaign.EmailsBounced,
            EmailsFailed = campaign.EmailsFailed,
            Unsubscribes = campaign.Unsubscribes,
            SpamComplaints = campaign.SpamComplaints,
            ScheduledStartDate = campaign.ScheduledStartDate,
            ScheduledEndDate = campaign.ScheduledEndDate,
            TimeZone = campaign.TimeZone,
            BatchSize = campaign.BatchSize,
            MaxEmailsPerHour = campaign.MaxEmailsPerHour,
            MaxEmailsPerDay = campaign.MaxEmailsPerDay,
            IsAbTest = campaign.IsAbTest,
            IsDripCampaign = campaign.IsDripCampaign,
            IncludeUnsubscribeLink = campaign.IncludeUnsubscribeLink,
            EnableOpenTracking = campaign.EnableOpenTracking,
            EnableClickTracking = campaign.EnableClickTracking,
            CreatedAt = campaign.CreatedAt,
            StartedAt = campaign.StartedAt,
            CompletedAt = campaign.CompletedAt
        }).ToList();
    }
}

public record GetCampaignStatisticsQuery : IRequest<CampaignStatisticsDto>
{
    public Guid CampaignId { get; init; }
}

public class GetCampaignStatisticsQueryHandler : IRequestHandler<GetCampaignStatisticsQuery, CampaignStatisticsDto>
{
    private readonly IEmailCampaignRepository _campaignRepository;

    public GetCampaignStatisticsQueryHandler(IEmailCampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public async Task<CampaignStatisticsDto> Handle(GetCampaignStatisticsQuery request, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);

        if (campaign == null)
            throw new Exception($"Campaign {request.CampaignId} not found");

        var stats = new CampaignStatisticsDto
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            TotalRecipients = campaign.TotalRecipients,
            EmailsSent = campaign.EmailsSent,
            EmailsDelivered = campaign.EmailsDelivered,
            EmailsOpened = campaign.EmailsOpened,
            EmailsClicked = campaign.EmailsClicked,
            EmailsBounced = campaign.EmailsBounced,
            EmailsFailed = campaign.EmailsFailed,
            Unsubscribes = campaign.Unsubscribes,
            SpamComplaints = campaign.SpamComplaints,
            OpenRate = campaign.EmailsSent > 0 ? (double)campaign.EmailsOpened / campaign.EmailsSent * 100 : 0,
            ClickRate = campaign.EmailsSent > 0 ? (double)campaign.EmailsClicked / campaign.EmailsSent * 100 : 0,
            BounceRate = campaign.EmailsSent > 0 ? (double)campaign.EmailsBounced / campaign.EmailsSent * 100 : 0,
            UnsubscribeRate = campaign.EmailsSent > 0 ? (double)campaign.Unsubscribes / campaign.EmailsSent * 100 : 0
        };

        if (campaign.IsAbTest && campaign.Variants.Any())
        {
            stats.VariantStatistics = campaign.Variants.Select(v => new VariantStatisticsDto
            {
                VariantId = v.Id,
                Name = v.Name,
                SentCount = v.SentCount,
                OpenedCount = v.OpenedCount,
                ClickedCount = v.ClickedCount,
                OpenRate = v.GetOpenRate(),
                ClickRate = v.GetClickRate()
            }).ToList();
        }

        return stats;
    }
}
