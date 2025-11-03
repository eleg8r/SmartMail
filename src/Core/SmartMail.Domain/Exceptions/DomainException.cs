namespace SmartMail.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class EmailValidationException : DomainException
{
    public EmailValidationException(string message) : base(message)
    {
    }
}

public class CampaignValidationException : DomainException
{
    public CampaignValidationException(string message) : base(message)
    {
    }
}

public class TenantNotFoundException : DomainException
{
    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant with ID {tenantId} was not found")
    {
    }
}

public class InvalidOperationDomainException : DomainException
{
    public InvalidOperationDomainException(string message) : base(message)
    {
    }
}
