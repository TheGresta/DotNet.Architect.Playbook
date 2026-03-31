namespace Playbook.Security.IdP.Domain.Models;

public record PolicyEvaluationResult
{
    public bool IsAllowed { get; init; }
    public AuthRequirement Requirement { get; init; }
    public int RiskScore { get; init; }
    public string Reason { get; init; } = string.Empty;

    public enum AuthRequirement
    {
        None,           // Trust established (Silent Login)
        MfaRequired,    // Step-up authentication needed
        StepUpHardware, // Specific hardware key (FIDO2) required
        AccountLockout, // High risk: Block immediately
        PasswordReset   // Credential stuffing suspected
    }

    // Factory methods for clean domain logic
    public static PolicyEvaluationResult Allow() => new() { IsAllowed = true, Requirement = AuthRequirement.None };
    public static PolicyEvaluationResult Challenge(int score, string reason) =>
        new() { IsAllowed = false, Requirement = AuthRequirement.MfaRequired, RiskScore = score, Reason = reason };
}
