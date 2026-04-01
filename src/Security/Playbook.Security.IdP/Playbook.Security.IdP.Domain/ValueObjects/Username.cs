using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class Username : ValueObject
{
    public string Value { get; }
    public string Normalized { get; }

    private Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
            throw new DomainException("Username must be at least 3 characters.", "INVALID_USERNAME");

        Value = value.Trim();
        Normalized = Value.ToUpperInvariant();
    }

    public static Username Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Normalized;
    }
}
