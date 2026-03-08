namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class ValidationKeys
{
    private const string _prefix = LocalizationPrefixes.Validation;

    public const string Required = _prefix + "REQUIRED";
    public const string InvalidFormat = _prefix + "INVALID_FORMAT";
    public const string TooShort = _prefix + "TOO_SHORT";
    public const string ProviderBlocked = _prefix + "PROVIDER_BLOCKED";
}
