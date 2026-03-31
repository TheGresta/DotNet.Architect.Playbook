namespace Playbook.Security.IdP.Domain.ValueObjects;

// Represents a point-in-time security assessment
public record RiskScore(int Value, string Reason)
{
    public bool IsHighRisk => Value > 80;
}
