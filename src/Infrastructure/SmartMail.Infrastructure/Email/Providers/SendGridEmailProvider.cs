using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Enums;
using System.Text;

namespace SmartMail.Infrastructure.Email.Providers;

public class SendGridEmailProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string? DefaultFromAddress { get; set; }
    public string? DefaultFromName { get; set; }
}

public class SendGridEmailProvider : IEmailProvider
{
    private readonly SendGridEmailProviderOptions _options;
    private readonly ILogger<SendGridEmailProvider> _logger;
    private readonly SendGridClient _client;

    public SendGridEmailProvider(
        IOptions<SendGridEmailProviderOptions> options,
        ILogger<SendGridEmailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new SendGridClient(_options.ApiKey);
    }

    public EmailProviderType ProviderType => EmailProviderType.SendGrid;

    public async Task<bool> SendAsync(Domain.Entities.Email email, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = CreateSendGridMessage(email);
            var response = await _client.SendEmailAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email {EmailId} sent successfully via SendGrid", email.Id);
                return true;
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send email {EmailId} via SendGrid. Status: {StatusCode}, Body: {Body}",
                    email.Id, response.StatusCode, body);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email {EmailId} via SendGrid", email.Id);
            return false;
        }
    }

    public async Task<bool> SendBulkAsync(IEnumerable<Domain.Entities.Email> emails, CancellationToken cancellationToken = default)
    {
        var success = true;

        // SendGrid supports batch sending, but for simplicity we'll send one by one
        // In production, you could optimize this using SendGrid's batch API
        foreach (var email in emails)
        {
            var result = await SendAsync(email, cancellationToken);
            if (!result)
                success = false;
        }

        return success;
    }

    private SendGridMessage CreateSendGridMessage(Domain.Entities.Email email)
    {
        var message = new SendGridMessage();

        // From
        message.SetFrom(new EmailAddress(
            email.From.Address ?? _options.DefaultFromAddress,
            email.From.DisplayName ?? _options.DefaultFromName));

        // To
        message.AddTo(new EmailAddress(email.To.Address, email.To.DisplayName));

        // CC
        foreach (var cc in email.Cc)
        {
            message.AddCc(new EmailAddress(cc.Address, cc.DisplayName));
        }

        // BCC
        foreach (var bcc in email.Bcc)
        {
            message.AddBcc(new EmailAddress(bcc.Address, bcc.DisplayName));
        }

        // Subject
        message.SetSubject(email.Content.Subject);

        // Body
        message.AddContent(MimeType.Html, email.Content.HtmlBody);
        if (!string.IsNullOrEmpty(email.Content.TextBody))
        {
            message.AddContent(MimeType.Text, email.Content.TextBody);
        }

        // Attachments
        foreach (var attachment in email.Attachments)
        {
            var base64Content = Convert.ToBase64String(attachment.Content);
            message.AddAttachment(attachment.FileName, base64Content, attachment.ContentType);
        }

        // Custom headers for tracking
        if (!string.IsNullOrEmpty(email.TrackingId))
        {
            message.AddCustomArg("tracking_id", email.TrackingId);
            message.AddCustomArg("email_id", email.Id.ToString());
        }

        if (email.CampaignId.HasValue)
        {
            message.AddCustomArg("campaign_id", email.CampaignId.Value.ToString());
        }

        // Enable click and open tracking
        message.SetClickTracking(false, false); // We handle our own tracking
        message.SetOpenTracking(false); // We handle our own tracking

        return message;
    }
}
