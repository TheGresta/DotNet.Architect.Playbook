namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string ActionFailed = "ACTION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
}
