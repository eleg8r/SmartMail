namespace SmartMail.Application.Common.Interfaces;

public interface ITemplateEngine
{
    /// <summary>
    /// Processes a template with personalization data
    /// Supports tokens like {{FirstName}}, {{LastName}}, etc.
    /// </summary>
    string ProcessTemplate(string template, Dictionary<string, string> data);

    /// <summary>
    /// Processes conditional content in templates
    /// Example: {{#if PremiumUser}}Premium content{{/if}}
    /// </summary>
    string ProcessConditionalContent(string template, Dictionary<string, string> data);

    /// <summary>
    /// Validates template syntax
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateTemplateAsync(
        string template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds unsubscribe link to email content (for compliance)
    /// </summary>
    string AddUnsubscribeLink(string htmlContent, string unsubscribeUrl, Guid emailId);

    /// <summary>
    /// Adds required compliance footer (CAN-SPAM, CASL)
    /// </summary>
    string AddComplianceFooter(string htmlContent, string companyName, string companyAddress);
}
