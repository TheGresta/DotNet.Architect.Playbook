using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Abstraction.Exceptions;

/// <summary>
/// Provides a centralized factory for domain-specific business rule violations.
/// This static registry ensures consistent exception instantiation across the domain layer.
/// </summary>
public static class DomainErrors
{
    /// <summary>
    /// Creates a <see cref="BusinessRuleException"/> specifically for insufficient balance scenarios.
    /// </summary>
    /// <param name="amount">The numeric value of the requested transaction.</param>
    /// <param name="currency">The ISO currency code or symbol associated with the transaction.</param>
    /// <returns>A configured instance of <see cref="BusinessRuleException"/>.</returns>
    public static BusinessRuleException InsufficientFunds(decimal amount, string currency)
        => new(BusinessRuleKeys.InsufficientFunds, amount, currency);
}
