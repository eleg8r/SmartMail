using SmartMail.Domain.Common;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Entities;

public class Tenant : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    // Configuration
    public EmailProviderType DefaultEmailProvider { get; private set; }
    public int MaxAttachmentSizeInMb { get; private set; }
    public int MaxEmailsPerHour { get; private set; }
    public int MaxEmailsPerDay { get; private set; }
    public bool EnableOpenTracking { get; private set; }
    public bool EnableClickTracking { get; private set; }

    // API Settings
    public string ApiKey { get; private set; }
    public DateTime ApiKeyCreatedAt { get; private set; }
    public DateTime? ApiKeyExpiresAt { get; private set; }

    // Metadata
    public string? ContactEmail { get; private set; }
    public string? ContactName { get; private set; }
    public DateTime? SubscriptionStartDate { get; private set; }
    public DateTime? SubscriptionEndDate { get; private set; }

    private Tenant() { }

    private Tenant(
        Guid id,
        TenantId tenantId,
        string name,
        string? description = null)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = true;
        DefaultEmailProvider = EmailProviderType.Smtp;
        MaxAttachmentSizeInMb = 25;
        MaxEmailsPerHour = 1000;
        MaxEmailsPerDay = 10000;
        EnableOpenTracking = true;
        EnableClickTracking = true;
        ApiKey = GenerateApiKey();
        ApiKeyCreatedAt = DateTime.UtcNow;
    }

    public static Tenant Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));

        return new Tenant(Guid.NewGuid(), TenantId.CreateNew(), name, description);
    }

    public void UpdateConfiguration(
        EmailProviderType? defaultProvider = null,
        int? maxAttachmentSizeMb = null,
        int? maxEmailsPerHour = null,
        int? maxEmailsPerDay = null,
        bool? enableOpenTracking = null,
        bool? enableClickTracking = null)
    {
        if (defaultProvider.HasValue)
            DefaultEmailProvider = defaultProvider.Value;

        if (maxAttachmentSizeMb.HasValue && maxAttachmentSizeMb.Value > 0)
            MaxAttachmentSizeInMb = maxAttachmentSizeMb.Value;

        if (maxEmailsPerHour.HasValue && maxEmailsPerHour.Value > 0)
            MaxEmailsPerHour = maxEmailsPerHour.Value;

        if (maxEmailsPerDay.HasValue && maxEmailsPerDay.Value > 0)
            MaxEmailsPerDay = maxEmailsPerDay.Value;

        if (enableOpenTracking.HasValue)
            EnableOpenTracking = enableOpenTracking.Value;

        if (enableClickTracking.HasValue)
            EnableClickTracking = enableClickTracking.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegenerateApiKey()
    {
        ApiKey = GenerateApiKey();
        ApiKeyCreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetApiKeyExpiration(DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        ApiKeyExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsApiKeyValid()
    {
        if (!IsActive)
            return false;

        if (ApiKeyExpiresAt.HasValue && ApiKeyExpiresAt.Value <= DateTime.UtcNow)
            return false;

        return true;
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
