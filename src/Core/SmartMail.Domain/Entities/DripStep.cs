using SmartMail.Domain.Common;

namespace SmartMail.Domain.Entities;

public class DripStep : Entity<Guid>
{
    public Guid CampaignId { get; private set; }
    public int StepNumber { get; private set; }
    public string Name { get; private set; }
    public string Subject { get; private set; }
    public string HtmlContent { get; private set; }
    public string? TextContent { get; private set; }

    // Delay Configuration
    public int DelayInDays { get; private set; }
    public int DelayInHours { get; private set; }
    public int DelayInMinutes { get; private set; }

    // Trigger Conditions (optional - for advanced drip campaigns)
    public string? TriggerCondition { get; private set; } // JSON or expression

    // Statistics
    public int SentCount { get; private set; }
    public int OpenedCount { get; private set; }
    public int ClickedCount { get; private set; }

    private DripStep() { }

    private DripStep(
        Guid id,
        Guid campaignId,
        int stepNumber,
        string name,
        string subject,
        string htmlContent,
        string? textContent,
        int delayInDays,
        int delayInHours,
        int delayInMinutes,
        string? triggerCondition = null)
    {
        Id = id;
        CampaignId = campaignId;
        StepNumber = stepNumber;
        Name = name;
        Subject = subject;
        HtmlContent = htmlContent;
        TextContent = textContent;
        DelayInDays = delayInDays;
        DelayInHours = delayInHours;
        DelayInMinutes = delayInMinutes;
        TriggerCondition = triggerCondition;
    }

    public static DripStep Create(
        Guid campaignId,
        int stepNumber,
        string name,
        string subject,
        string htmlContent,
        string? textContent = null,
        int delayInDays = 0,
        int delayInHours = 0,
        int delayInMinutes = 0,
        string? triggerCondition = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Step name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));

        if (delayInDays < 0 || delayInHours < 0 || delayInMinutes < 0)
            throw new ArgumentException("Delay values cannot be negative");

        return new DripStep(
            Guid.NewGuid(),
            campaignId,
            stepNumber,
            name,
            subject,
            htmlContent,
            textContent,
            delayInDays,
            delayInHours,
            delayInMinutes,
            triggerCondition);
    }

    public TimeSpan GetTotalDelay()
    {
        return new TimeSpan(DelayInDays, DelayInHours, DelayInMinutes, 0);
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
}
