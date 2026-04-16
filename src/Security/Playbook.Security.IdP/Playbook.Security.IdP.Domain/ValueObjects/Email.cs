using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }
    public string Normalized { get; }

    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new DomainException("Invalid email format.", "INVALID_EMAIL");

        Value = value.Trim();
        Normalized = Value.ToUpperInvariant();
    }

    public static Email Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Normalized;
    }
}
