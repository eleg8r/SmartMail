namespace SmartMail.Domain.Enums;

public enum EmailProviderType
{
    Smtp = 0,
    SendGrid = 1,
    AwsSes = 2,
    Mailgun = 3,
    Custom = 4
}
