namespace Playbook.Exceptions.Constants;

/// <summary>
/// Defines keys for detailed error messages and templates.
/// These are typically used to fetch verbose descriptions from localization resources.
/// </summary>
public static class DetailKeys
{
    private const string P = LocalizationPrefixes.Detail;

    public const string NotFound = P + "NOT_FOUND";
    public const string UnexpectedError = P + "UNEXPECTED_ERROR";
    public const string ValidationSummary = P + "VALIDATION_SUMMARY";
    public const string Unauthorized = P + "UNAUTHORIZED";
    public const string Forbidden = P + "FORBIDDEN";
    public const string BusinessRule = P + "BUSINESS_RULE";
}
