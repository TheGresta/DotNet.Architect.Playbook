namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class BusinessRuleKeys
{
    private const string _prefix = LocalizationPrefixes.Rule;

    public const string InsufficientFunds = _prefix + "INSUFFICIENT_FUNDS";
    public const string AccountLocked = _prefix + "ACCOUNT_LOCKED";
}
