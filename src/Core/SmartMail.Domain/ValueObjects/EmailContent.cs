using SmartMail.Domain.Common;

namespace SmartMail.Domain.ValueObjects;

public sealed class EmailContent : ValueObject
{
    public string Subject { get; private set; }
    public string HtmlBody { get; private set; }
    public string? TextBody { get; private set; }

    private EmailContent(string subject, string htmlBody, string? textBody)
    {
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
    }

    public static EmailContent Create(string subject, string htmlBody, string? textBody = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject cannot be empty", nameof(subject));

        if (string.IsNullOrWhiteSpace(htmlBody))
            throw new ArgumentException("Email body cannot be empty", nameof(htmlBody));

        return new EmailContent(subject, htmlBody, textBody);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Subject;
        yield return HtmlBody;
        if (TextBody != null)
            yield return TextBody;
    }
}
