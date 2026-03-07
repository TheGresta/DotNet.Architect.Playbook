using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public static class DomainErrors
{
    public static BusinessRuleException InsufficientFunds(decimal amount, string currency)
        => new(BusinessRuleKeys.InsufficientFunds, amount, currency);
}
