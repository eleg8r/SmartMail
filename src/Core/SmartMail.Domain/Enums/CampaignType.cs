namespace SmartMail.Domain.Enums;

public enum CampaignType
{
    OneTime = 0,        // Send all emails at once
    Scheduled = 1,      // Send emails at scheduled times in batches
    Drip = 2,          // Automated sequence based on triggers
    ABTest = 3         // A/B testing campaign
}
