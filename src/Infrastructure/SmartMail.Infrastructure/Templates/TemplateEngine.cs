using Microsoft.Extensions.Logging;
using SmartMail.Application.Common.Interfaces;
using System.Text.RegularExpressions;

namespace SmartMail.Infrastructure.Templates;

public class TemplateEngine : ITemplateEngine
{
    private readonly ILogger<TemplateEngine> _logger;
    private const string TokenPattern = @"\{\{([^}]+)\}\}";
    private const string ConditionalPattern = @"\{\{#if\s+([^}]+)\}\}(.*?)\{\{/if\}\}";

    public TemplateEngine(ILogger<TemplateEngine> logger)
    {
        _logger = logger;
    }

    public string ProcessTemplate(string template, Dictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        // Replace all tokens with values from data
        var result = Regex.Replace(template, TokenPattern, match =>
        {
            var key = match.Groups[1].Value.Trim();

            if (data.TryGetValue(key, out var value))
            {
                return value;
            }

            // If key not found, return empty string or keep the token
            _logger.LogWarning("Template token {Token} not found in data", key);
            return string.Empty;
        });

        return result;
    }

    public string ProcessConditionalContent(string template, Dictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        // Process conditional blocks
        var result = Regex.Replace(template, ConditionalPattern, match =>
        {
            var condition = match.Groups[1].Value.Trim();
            var content = match.Groups[2].Value;

            // Evaluate condition
            var shouldInclude = EvaluateCondition(condition, data);

            return shouldInclude ? content : string.Empty;
        }, RegexOptions.Singleline);

        // After processing conditionals, process regular tokens
        result = ProcessTemplate(result, data);

        return result;
    }

    public Task<(bool IsValid, List<string> Errors)> ValidateTemplateAsync(
        string template,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(template))
        {
            errors.Add("Template cannot be empty");
            return Task.FromResult((false, errors));
        }

        // Check for balanced conditional blocks
        var ifCount = Regex.Matches(template, @"\{\{#if\s+").Count;
        var endIfCount = Regex.Matches(template, @"\{\{/if\}\}").Count;

        if (ifCount != endIfCount)
        {
            errors.Add($"Unbalanced conditional blocks: {ifCount} #if statements but {endIfCount} /if statements");
        }

        // Check for valid token syntax
        var tokens = Regex.Matches(template, TokenPattern);
        foreach (Match token in tokens)
        {
            var tokenName = token.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                errors.Add($"Empty token found at position {token.Index}");
            }
        }

        var isValid = errors.Count == 0;
        return Task.FromResult((isValid, errors));
    }

    public string AddUnsubscribeLink(string htmlContent, string unsubscribeUrl, Guid emailId)
    {
        var fullUnsubscribeUrl = $"{unsubscribeUrl}?emailId={emailId}";

        var unsubscribeHtml = $@"
<div style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; font-size: 12px; color: #666;"">
    <p>If you no longer wish to receive these emails, you can <a href=""{fullUnsubscribeUrl}"" style=""color: #666; text-decoration: underline;"">unsubscribe here</a>.</p>
</div>";

        // Add before closing body tag
        if (htmlContent.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return htmlContent.Replace("</body>", $"{unsubscribeHtml}</body>", StringComparison.OrdinalIgnoreCase);
        }

        // If no body tag, append to the end
        return htmlContent + unsubscribeHtml;
    }

    public string AddComplianceFooter(string htmlContent, string companyName, string companyAddress)
    {
        var complianceFooter = $@"
<div style=""margin-top: 20px; padding: 15px; background-color: #f5f5f5; border-top: 1px solid #ddd; font-size: 11px; color: #666; text-align: center;"">
    <p><strong>{companyName}</strong></p>
    <p>{companyAddress}</p>
    <p>This email was sent in compliance with the CAN-SPAM Act and CASL regulations.</p>
    <p>You are receiving this email because you opted in to receive communications from {companyName}.</p>
</div>";

        // Add before unsubscribe link if it exists, otherwise before closing body tag
        if (htmlContent.Contains("unsubscribe here</a>", StringComparison.OrdinalIgnoreCase))
        {
            var unsubIndex = htmlContent.LastIndexOf("<div style=\"margin-top: 30px; padding-top: 20px;", StringComparison.OrdinalIgnoreCase);
            if (unsubIndex > 0)
            {
                return htmlContent.Insert(unsubIndex, complianceFooter);
            }
        }

        if (htmlContent.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return htmlContent.Replace("</body>", $"{complianceFooter}</body>", StringComparison.OrdinalIgnoreCase);
        }

        return htmlContent + complianceFooter;
    }

    private bool EvaluateCondition(string condition, Dictionary<string, string> data)
    {
        // Simple condition evaluation
        // Supports: VariableName, !VariableName

        condition = condition.Trim();

        // Check for negation
        if (condition.StartsWith("!"))
        {
            var varName = condition.Substring(1).Trim();
            return !data.ContainsKey(varName) || string.IsNullOrEmpty(data[varName]) || data[varName].ToLower() == "false";
        }

        // Simple existence check
        if (data.TryGetValue(condition, out var value))
        {
            return !string.IsNullOrEmpty(value) && value.ToLower() != "false";
        }

        return false;
    }
}
