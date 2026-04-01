using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class IpAddress : ValueObject
{
    public string Value { get; }

    private IpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("IP Address cannot be empty.", "INVALID_IP");

        // Basic validation: checks if it's a valid IPv4 or IPv6 format
        if (!System.Net.IPAddress.TryParse(value, out _))
            throw new DomainException($"Invalid IP Address format: {value}", "INVALID_IP_FORMAT");

        Value = value.Trim();
    }

    public static IpAddress Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(IpAddress ip) => ip.Value;
}
