using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Exceptions;

/// <summary>
/// Thrown when a security policy (Risk, Geo-fencing, IP Blacklist) 
/// prevents an operation from proceeding.
/// </summary>
public sealed class IdentityPolicyViolationException : DomainException
{
    public string PolicyName { get; }
    public RiskScore? Risk { get; }
    public string? Evidence { get; }

    public IdentityPolicyViolationException(
        string policyName,
        string message,
        RiskScore? risk = null,
        string? evidence = null)
        : base(message, "POLICY_VIOLATION")
    {
        PolicyName = policyName;
        Risk = risk;
        Evidence = evidence;
    }
}
