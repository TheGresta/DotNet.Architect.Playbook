namespace Playbook.Exceptions.Constants;

public static class ValidationKeys
{
    private const string P = LocalizationPrefixes.Validation;

    public const string Required = P + "REQUIRED";
    public const string InvalidFormat = P + "INVALID_FORMAT";
    public const string TooShort = P + "TOO_SHORT";
    public const string ProviderBlocked = P + "PROVIDER_BLOCKED";
}