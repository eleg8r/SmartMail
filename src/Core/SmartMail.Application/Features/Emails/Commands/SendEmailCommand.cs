using MediatR;
using Microsoft.Extensions.Logging;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Application.DTOs;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Application.Features.Emails.Commands;

public record SendEmailCommand : IRequest<SendEmailResultDto>
{
    public CreateEmailDto Email { get; init; } = null!;
    public bool SendImmediately { get; init; } = true;
    public EmailProviderType? ProviderOverride { get; init; }
}

public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, SendEmailResultDto>
{
    private readonly IEmailRepository _emailRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IEmailService _emailService;
    private readonly ISchedulerService _schedulerService;
    private readonly ITrackingService _trackingService;
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(
        IEmailRepository emailRepository,
        ITenantRepository tenantRepository,
        IEmailService emailService,
        ISchedulerService schedulerService,
        ITrackingService trackingService,
        ITemplateEngine templateEngine,
        ILogger<SendEmailCommandHandler> logger)
    {
        _emailRepository = emailRepository;
        _tenantRepository = tenantRepository;
        _emailService = emailService;
        _schedulerService = schedulerService;
        _trackingService = trackingService;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task<SendEmailResultDto> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Email;

            // Create email entity
            var from = EmailAddress.Create(dto.From, dto.FromDisplayName);
            var to = EmailAddress.Create(dto.To, dto.ToDisplayName);
            var content = EmailContent.Create(dto.Subject, dto.HtmlBody, dto.TextBody);
            var tenantId = TenantId.Create(dto.TenantId);

            var email = Email.Create(
                tenantId,
                from,
                to,
                content,
                dto.CampaignId,
                dto.ScheduledAt);

            // Add CC and BCC
            foreach (var cc in dto.Cc)
            {
                email.AddCc(EmailAddress.Create(cc));
            }

            foreach (var bcc in dto.Bcc)
            {
                email.AddBcc(EmailAddress.Create(bcc));
            }

            // Get tenant to retrieve max attachment size
            var tenant = await _tenantRepository.GetByIdAsync(dto.TenantId, cancellationToken);

            if (tenant == null)
                throw new Exception($"Tenant {dto.TenantId} not found");

            var maxSizeBytes = tenant.MaxAttachmentSizeInMb * 1024 * 1024;

            // Add attachments
            foreach (var attachmentDto in dto.Attachments)
            {
                var attachment = EmailAttachment.Create(
                    attachmentDto.FileName,
                    attachmentDto.ContentType,
                    attachmentDto.Content,
                    email.Id,
                    maxSizeBytes);

                email.AddAttachment(attachment);
            }

            // Add metadata
            foreach (var metadata in dto.Metadata)
            {
                email.AddMetadata(metadata.Key, metadata.Value);
            }

            // Save to database
            await _emailRepository.AddAsync(email, cancellationToken);

            // Send immediately or schedule
            if (request.SendImmediately && !dto.ScheduledAt.HasValue)
            {
                var success = await _emailService.SendEmailAsync(
                    email,
                    request.ProviderOverride,
                    cancellationToken);

                return new SendEmailResultDto
                {
                    EmailId = email.Id,
                    Success = success,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = success ? null : "Failed to send email"
                };
            }
            else
            {
                var scheduledTime = dto.ScheduledAt ?? DateTime.UtcNow;
                await _schedulerService.ScheduleEmailAsync(email.Id, scheduledTime, cancellationToken);

                return new SendEmailResultDto
                {
                    EmailId = email.Id,
                    Success = true,
                    SentAt = scheduledTime
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");

            return new SendEmailResultDto
            {
                EmailId = Guid.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
