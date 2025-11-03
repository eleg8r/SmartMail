using SmartMail.Domain.Common;
using System.Text.RegularExpressions;

namespace SmartMail.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Address { get; private set; }
    public string? DisplayName { get; private set; }

    private EmailAddress(string address, string? displayName = null)
    {
        Address = address;
        DisplayName = displayName;
    }

    public static EmailAddress Create(string address, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Email address cannot be empty", nameof(address));

        if (!EmailRegex.IsMatch(address))
            throw new ArgumentException($"Invalid email address: {address}", nameof(address));

        return new EmailAddress(address.ToLowerInvariant(), displayName);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName)
            ? Address
            : $"{DisplayName} <{Address}>";
    }
}
