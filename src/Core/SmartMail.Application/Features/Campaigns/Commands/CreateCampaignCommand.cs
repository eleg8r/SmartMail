using MediatR;
using Microsoft.Extensions.Logging;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Application.DTOs;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Application.Features.Campaigns.Commands;

public record CreateCampaignCommand : IRequest<CampaignDto>
{
    public CreateCampaignDto Campaign { get; init; } = null!;
}

public class CreateCampaignCommandHandler : IRequestHandler<CreateCampaignCommand, CampaignDto>
{
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<CreateCampaignCommandHandler> _logger;

    public CreateCampaignCommandHandler(
        IEmailCampaignRepository campaignRepository,
        ITemplateEngine templateEngine,
        ILogger<CreateCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task<CampaignDto> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Campaign;

        // Validate template
        var (isValid, errors) = await _templateEngine.ValidateTemplateAsync(dto.HtmlTemplate, cancellationToken);
        if (!isValid)
        {
            throw new ArgumentException($"Invalid template: {string.Join(", ", errors)}");
        }

        // Create campaign
        var tenantId = TenantId.Create(dto.TenantId);
        var fromAddress = EmailAddress.Create(dto.From, dto.FromDisplayName);

        var campaign = EmailCampaign.Create(
            tenantId,
            dto.Name,
            dto.Description,
            dto.Type,
            fromAddress,
            dto.Subject,
            dto.HtmlTemplate,
            dto.TextTemplate);

        // Add recipients
        foreach (var recipientEmail in dto.Recipients)
        {
            Dictionary<string, string>? personalizationData = null;
            if (dto.RecipientPersonalization.TryGetValue(recipientEmail, out var data))
            {
                personalizationData = data;
            }
            campaign.AddRecipient(recipientEmail, personalizationData);
        }

        // Add recipient lists
        foreach (var listId in dto.RecipientListIds)
        {
            campaign.AddRecipientList(listId);
        }

        // Configure scheduling
        if (dto.ScheduledStartDate.HasValue)
        {
            campaign.SetSchedule(dto.ScheduledStartDate.Value, dto.ScheduledEndDate, dto.TimeZone);
        }

        // Configure batching
        if (dto.BatchSize.HasValue && dto.BatchSchedules.Any())
        {
            var batchSchedules = dto.BatchSchedules.Select(bs =>
                BatchSchedule.Create(campaign.Id, bs.BatchNumber, bs.ScheduledTime, bs.MaxRecipients)
            ).ToList();

            campaign.ConfigureBatching(dto.BatchSize.Value, batchSchedules);
        }

        // Configure rate limiting
        campaign.SetRateLimits(dto.MaxEmailsPerHour, dto.MaxEmailsPerDay);

        // Configure A/B testing
        if (dto.Variants != null && dto.Variants.Any())
        {
            var variants = dto.Variants.Select(v =>
                CampaignVariant.Create(
                    campaign.Id,
                    v.Name,
                    v.Subject,
                    v.HtmlContent,
                    v.TextContent,
                    v.Weight)
            ).ToList();

            campaign.ConfigureAbTest(
                variants,
                dto.AbTestSampleSize ?? 100,
                dto.AbTestEndDate ?? DateTime.UtcNow.AddDays(7));
        }

        // Configure drip campaign
        if (dto.DripSteps != null && dto.DripSteps.Any())
        {
            var dripSteps = dto.DripSteps.Select(ds =>
                DripStep.Create(
                    campaign.Id,
                    ds.StepNumber,
                    ds.Name,
                    ds.Subject,
                    ds.HtmlContent,
                    ds.TextContent,
                    ds.DelayInDays,
                    ds.DelayInHours,
                    ds.DelayInMinutes,
                    ds.TriggerCondition)
            ).ToList();

            campaign.ConfigureDripCampaign(dripSteps);
        }

        // Set unsubscribe URL
        if (!string.IsNullOrEmpty(dto.UnsubscribeUrl))
        {
            campaign.SetUnsubscribeUrl(dto.UnsubscribeUrl);
        }

        // Save to database
        await _campaignRepository.AddAsync(campaign, cancellationToken);

        _logger.LogInformation("Created campaign {CampaignId} - {CampaignName}", campaign.Id, campaign.Name);

        return MapToDto(campaign);
    }

    private CampaignDto MapToDto(EmailCampaign campaign)
    {
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
