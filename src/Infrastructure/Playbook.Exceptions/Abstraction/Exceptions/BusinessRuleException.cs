using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

/// <summary>
/// Represents an exception thrown when a specific domain business rule is violated.
/// </summary>
/// <param name="ruleKey">A unique string key used for localization and identification of the rule.</param>
/// <param name="args">Optional contextual arguments to be interpolated into the error message.</param>
public sealed class BusinessRuleException(string ruleKey, params object[] args)
    : DomainException(ErrorCodes.BusinessRuleViolation)
{
    /// <summary>
    /// Gets the identifier key for the violated business rule.
    /// </summary>
    public string RuleKey { get; } = ruleKey;

    /// <summary>
    /// Gets the contextual arguments associated with the rule violation.
    /// </summary>
    public object[] Args { get; } = args;

    /// <summary>
    /// Dispatches the exception to the appropriate mapping logic using the Visitor pattern.
    /// </summary>
    /// <param name="mapper">The implementation of <see cref="IExceptionMapper"/>.</param>
    /// <returns>The result of the mapping operation.</returns>
    public override ExceptionMappingResult Map(IExceptionMapper mapper)
        => mapper.MapSpecific(this);
}
