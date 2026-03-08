namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class TitleKeys
{
    private const string _prefix = LocalizationPrefixes.Info;

    public const string NotFound = _prefix + "NOT_FOUND";
    public const string ValidationError = _prefix + "VALIDATION_ERROR";
    public const string BusinessRule = _prefix + "BUSINESS_RULE";
    public const string InternalServer = _prefix + "INTERNAL_SERVER";
    public const string Unauthorized = _prefix + "UNAUTHORIZED";
    public const string Forbidden = _prefix + "FORBIDDEN";
}
