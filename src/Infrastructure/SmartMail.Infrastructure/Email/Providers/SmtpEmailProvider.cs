using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;

namespace SmartMail.Infrastructure.Email.Providers;

public class SmtpEmailProviderOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public bool UseStartTls { get; set; } = true;
    public string? DefaultFromAddress { get; set; }
    public string? DefaultFromName { get; set; }
}

public class SmtpEmailProvider : IEmailProvider
{
    private readonly SmtpEmailProviderOptions _options;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        IOptions<SmtpEmailProviderOptions> options,
        ILogger<SmtpEmailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public EmailProviderType ProviderType => EmailProviderType.Smtp;

    public async Task<bool> SendAsync(Domain.Entities.Email email, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = CreateMimeMessage(email);

            using var client = new SmtpClient();

            // Connect to SMTP server
            var secureSocketOptions = _options.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : _options.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            // Send the email
            await client.SendAsync(message, cancellationToken);

            // Disconnect
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email {EmailId} sent successfully via SMTP", email.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email {EmailId} via SMTP", email.Id);
            return false;
        }
    }

    public async Task<bool> SendBulkAsync(IEnumerable<Domain.Entities.Email> emails, CancellationToken cancellationToken = default)
    {
        var success = true;

        using var client = new SmtpClient();

        try
        {
            // Connect once for all emails
            var secureSocketOptions = _options.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : _options.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            foreach (var email in emails)
            {
                try
                {
                    var message = CreateMimeMessage(email);
                    await client.SendAsync(message, cancellationToken);
                    _logger.LogInformation("Bulk email {EmailId} sent successfully via SMTP", email.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send bulk email {EmailId} via SMTP", email.Id);
                    success = false;
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SMTP server for bulk sending");
            return false;
        }

        return success;
    }

    private MimeMessage CreateMimeMessage(Domain.Entities.Email email)
    {
        var message = new MimeMessage();

        // From
        message.From.Add(new MailboxAddress(
            email.From.DisplayName ?? _options.DefaultFromName,
            email.From.Address ?? _options.DefaultFromAddress));

        // To
        message.To.Add(new MailboxAddress(email.To.DisplayName, email.To.Address));

        // CC
        foreach (var cc in email.Cc)
        {
            message.Cc.Add(new MailboxAddress(cc.DisplayName, cc.Address));
        }

        // BCC
        foreach (var bcc in email.Bcc)
        {
            message.Bcc.Add(new MailboxAddress(bcc.DisplayName, bcc.Address));
        }

        // Subject
        message.Subject = email.Content.Subject;

        // Body
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = email.Content.HtmlBody,
            TextBody = email.Content.TextBody
        };

        // Attachments
        foreach (var attachment in email.Attachments)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType));
        }

        message.Body = bodyBuilder.ToMessageBody();

        // Add custom headers for tracking
        if (!string.IsNullOrEmpty(email.TrackingId))
        {
            message.Headers.Add("X-SmartMail-Tracking-ID", email.TrackingId);
            message.Headers.Add("X-SmartMail-Email-ID", email.Id.ToString());
        }

        if (email.CampaignId.HasValue)
        {
            message.Headers.Add("X-SmartMail-Campaign-ID", email.CampaignId.Value.ToString());
        }

        return message;
    }
}
