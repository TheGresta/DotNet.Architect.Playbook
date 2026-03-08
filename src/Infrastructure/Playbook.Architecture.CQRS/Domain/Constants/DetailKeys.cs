namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class DetailKeys
{
    private const string _prefix = LocalizationPrefixes.Detail;

    public const string NotFound = _prefix + "NOT_FOUND";
    public const string UnexpectedError = _prefix + "UNEXPECTED_ERROR";
    public const string ValidationSummary = _prefix + "VALIDATION_SUMMARY";
    public const string Unauthorized = _prefix + "UNAUTHORIZED";
    public const string Forbidden = _prefix + "FORBIDDEN";
    public const string BusinessRule = _prefix + "BUSINESS_RULE";
}
