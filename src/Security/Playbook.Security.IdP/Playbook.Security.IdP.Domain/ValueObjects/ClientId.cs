using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class ClientId : ValueObject
{
    public string Value { get; }

    private ClientId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Client ID cannot be empty.", "INVALID_CLIENT_ID");

        Value = value.Trim();
    }

    public static ClientId Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(ClientId clientId) => clientId.Value;
}
