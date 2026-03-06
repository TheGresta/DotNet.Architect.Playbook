namespace Playbook.Exceptions.Constants;

public static class TitleKeys
{
    private const string P = LocalizationPrefixes.Info;

    public const string NotFound = P + "NOT_FOUND";
    public const string ValidationError = P + "VALIDATION_ERROR";
    public const string BusinessRule = P + "BUSINESS_RULE";
    public const string InternalServer = P + "INTERNAL_SERVER";
    public const string Unauthorized = P + "UNAUTHORIZED";
}
