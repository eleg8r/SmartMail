using SmartMail.Domain.Common;

namespace SmartMail.Domain.Entities;

public class CampaignVariant : Entity<Guid>
{
    public Guid CampaignId { get; private set; }
    public string Name { get; private set; }
    public string Subject { get; private set; }
    public string HtmlContent { get; private set; }
    public string? TextContent { get; private set; }
    public int Weight { get; private set; } // For distribution (e.g., 50% = 50)

    // Statistics
    public int SentCount { get; private set; }
    public int OpenedCount { get; private set; }
    public int ClickedCount { get; private set; }
    public int BouncedCount { get; private set; }

    private CampaignVariant() { }

    private CampaignVariant(
        Guid id,
        Guid campaignId,
        string name,
        string subject,
        string htmlContent,
        string? textContent,
        int weight)
    {
        Id = id;
        CampaignId = campaignId;
        Name = name;
        Subject = subject;
        HtmlContent = htmlContent;
        TextContent = textContent;
        Weight = weight;
    }

    public static CampaignVariant Create(
        Guid campaignId,
        string name,
        string subject,
        string htmlContent,
        string? textContent = null,
        int weight = 50)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));

        if (weight <= 0 || weight > 100)
            throw new ArgumentException("Weight must be between 1 and 100", nameof(weight));

        return new CampaignVariant(Guid.NewGuid(), campaignId, name, subject, htmlContent, textContent, weight);
    }

    public void RecordSent()
    {
        SentCount++;
    }

    public void RecordOpened()
    {
        OpenedCount++;
    }

    public void RecordClicked()
    {
        ClickedCount++;
    }

    public void RecordBounced()
    {
        BouncedCount++;
    }

    public double GetOpenRate()
    {
        return SentCount > 0 ? (double)OpenedCount / SentCount * 100 : 0;
    }

    public double GetClickRate()
    {
        return SentCount > 0 ? (double)ClickedCount / SentCount * 100 : 0;
    }

    public double GetBounceRate()
    {
        return SentCount > 0 ? (double)BouncedCount / SentCount * 100 : 0;
    }
}
