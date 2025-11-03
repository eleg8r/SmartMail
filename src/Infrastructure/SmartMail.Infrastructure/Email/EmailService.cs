using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;

namespace SmartMail.Infrastructure.Email;

public class EmailServiceOptions
{
    public EmailProviderType DefaultProvider { get; set; } = EmailProviderType.Smtp;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class EmailService : IEmailService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailServiceOptions _options;
    private readonly ILogger<EmailService> _logger;
    private readonly Dictionary<EmailProviderType, Type> _providerTypes;

    public EmailService(
        IServiceProvider serviceProvider,
        IOptions<EmailServiceOptions> options,
        ILogger<EmailService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;

        // Register provider types
        _providerTypes = new Dictionary<EmailProviderType, Type>();
    }

    public async Task<bool> SendEmailAsync(
        Domain.Entities.Email email,
        EmailProviderType? providerOverride = null,
        CancellationToken cancellationToken = default)
    {
        var providerType = providerOverride ?? _options.DefaultProvider;
        var provider = GetProvider(providerType);

        if (provider == null)
        {
            _logger.LogError("Email provider {ProviderType} not found", providerType);
            return false;
        }

        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                var success = await provider.SendAsync(email, cancellationToken);

                if (success)
                {
                    email.MarkAsSent(providerType);
                    return true;
                }

                retryCount++;

                if (retryCount <= _options.MaxRetries)
                {
                    await Task.Delay(_options.RetryDelayMs * retryCount, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Retry {RetryCount} failed for email {EmailId}", retryCount, email.Id);
                retryCount++;

                if (retryCount <= _options.MaxRetries)
                {
                    await Task.Delay(_options.RetryDelayMs * retryCount, cancellationToken);
                }
            }
        }

        email.MarkAsFailed(lastException?.Message ?? "Failed to send email after retries");
        return false;
    }

    public async Task<bool> SendBulkEmailsAsync(
        IEnumerable<Domain.Entities.Email> emails,
        EmailProviderType? providerOverride = null,
        CancellationToken cancellationToken = default)
    {
        var providerType = providerOverride ?? _options.DefaultProvider;
        var provider = GetProvider(providerType);

        if (provider == null)
        {
            _logger.LogError("Email provider {ProviderType} not found", providerType);
            return false;
        }

        try
        {
            var success = await provider.SendBulkAsync(emails, cancellationToken);

            if (success)
            {
                foreach (var email in emails)
                {
                    email.MarkAsSent(providerType);
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk emails");

            foreach (var email in emails)
            {
                email.MarkAsFailed(ex.Message);
            }

            return false;
        }
    }

    private IEmailProvider? GetProvider(EmailProviderType providerType)
    {
        // Get all registered providers
        var providers = _serviceProvider.GetServices<IEmailProvider>();
        return providers.FirstOrDefault(p => p.ProviderType == providerType);
    }
}
