using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;

namespace SmartMail.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(
        Email email,
        EmailProviderType? providerOverride = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendBulkEmailsAsync(
        IEnumerable<Email> emails,
        EmailProviderType? providerOverride = null,
        CancellationToken cancellationToken = default);
}

public interface IEmailProvider
{
    EmailProviderType ProviderType { get; }

    Task<bool> SendAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task<bool> SendBulkAsync(
        IEnumerable<Email> emails,
        CancellationToken cancellationToken = default);
}
