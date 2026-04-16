using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class CorrelationId : ValueObject
{
    public string Value { get; }

    private CorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Correlation ID is required for tracking the auth flow.", "INVALID_CORRELATION_ID");

        Value = value.Trim();
    }

    public static CorrelationId Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
