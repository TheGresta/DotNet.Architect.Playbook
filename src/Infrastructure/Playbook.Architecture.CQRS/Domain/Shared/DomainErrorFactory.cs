using ErrorOr;

using Playbook.Architecture.CQRS.Domain.Constants;

namespace Playbook.Architecture.CQRS.Domain.Shared;

public static class DomainErrorFactory
{
    // 1. Business Rule Errors
    public static Error BusinessRule(string ruleKey, params object[] args) =>
        Error.Conflict(
            code: ErrorCodes.BusinessRuleViolation,
            description: DetailKeys.BusinessRule,
            metadata: new Dictionary<string, object> { { MetadataKeys.RuleKey, ruleKey }, { MetadataKeys.Args, args } }
        );

    // 2. Not Found Errors
    public static Error NotFound(string resourceName, string resourceKey) =>
        Error.NotFound(
            code: ErrorCodes.NotFound,
            description: DetailKeys.NotFound,
            metadata: new Dictionary<string, object> { { MetadataKeys.ResourceName, resourceName }, { MetadataKeys.ResourceKey, resourceKey } }
        );

    // 3. Validation Errors
    public static Error Validation(Dictionary<string, ValidationError[]> errors) =>
        Error.Validation(
            code: ErrorCodes.ValidationError,
            description: DetailKeys.ValidationSummary,
            metadata: new Dictionary<string, object> { { MetadataKeys.ValidationFailures, errors } }
        );
}
