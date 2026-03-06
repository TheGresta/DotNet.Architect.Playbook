using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public static class DomainErrors
{
    public static BusinessRuleException InsufficientFunds(decimal amount, string cur)
        => new(BusinessRuleKeys.InsufficientFunds, amount, cur);
}
