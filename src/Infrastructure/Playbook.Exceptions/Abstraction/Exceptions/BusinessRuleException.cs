using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public sealed class BusinessRuleException(string ruleKey, params object[] args)
    : DomainException(ErrorCodes.BusinessRuleViolation)
{
    public string RuleKey { get; } = ruleKey;
    public object[] Args { get; } = args;
    public override ExceptionMappingResult Map(IExceptionMapper mapper)
        => mapper.MapSpecific(this);
}
