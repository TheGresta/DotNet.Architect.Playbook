namespace Playbook.Exceptions.Constants;

public static class DetailKeys
{
    private const string P = LocalizationPrefixes.Detail;

    public const string NotFound = P + "NOT_FOUND";
    public const string UnexpectedError = P + "UNEXPECTED_ERROR";
    public const string ValidationSummary = P + "VALIDATION_SUMMARY";
    public const string Unauthorized = P + "UNAUTHORIZED";
}
