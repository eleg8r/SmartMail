using SmartMail.Domain.Common;

namespace SmartMail.Domain.ValueObjects;

public sealed class TenantId : ValueObject
{
    public Guid Value { get; private set; }

    private TenantId(Guid value)
    {
        Value = value;
    }

    public static TenantId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty", nameof(value));

        return new TenantId(value);
    }

    public static TenantId CreateNew()
    {
        return new TenantId(Guid.NewGuid());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(TenantId tenantId) => tenantId.Value;
}
