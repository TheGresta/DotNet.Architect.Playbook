namespace Playbook.Exceptions.Constants;

/// <summary>
/// Contains unique keys representing specific business rule violations.
/// These keys are prefixed to facilitate localization lookups in the domain layer.
/// </summary>
public static class BusinessRuleKeys
{
    private const string P = LocalizationPrefixes.Rule;

    /// <summary>Represents a failure due to inadequate account balance.</summary>
    public const string InsufficientFunds = P + "INSUFFICIENT_FUNDS";

    /// <summary>Represents a failure because the target account is in a locked state.</summary>
    public const string AccountLocked = P + "ACCOUNT_LOCKED";
}
