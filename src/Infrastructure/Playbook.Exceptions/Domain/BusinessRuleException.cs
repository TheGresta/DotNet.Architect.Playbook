using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Domain;

public class BusinessRuleException(string ruleKey, params object[] args)
    : DomainException(ErrorCodes.BusinessRuleViolation)
{
    public string RuleKey { get; } = ruleKey;
    public object[] Args { get; } = args;
}