using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

// Represents a point-in-time security assessment
public sealed class RiskScore : ValueObject
{
    public int Value { get; }
    public string Reason { get; }
    public bool IsHighRisk => Value > 80;

    public RiskScore(int value, string reason)
    {
        if (value < 0 || value > 100)
            throw new DomainException("Risk score must be between 0 and 100.");

        Value = value;
        Reason = reason ?? "No reason provided";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Reason;
    }
}
