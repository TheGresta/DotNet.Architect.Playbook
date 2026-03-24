using FluentValidation;

using Playbook.Architecture.CQRS.Domain.ValueObjects;

namespace Playbook.Architecture.CQRS.Application.Common.Extensions;

/// <summary>
/// Provides specialized FluentValidation extensions to bridge domain-driven Design (DDD) Value Objects 
/// with request validation logic. These methods ensure that primitive inputs adhere to the 
/// internal invariants defined within the domain layer before a request reaches the handler.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates a decimal value using the domain's <see cref="Price"/> Value Object logic.
    /// This ensures consistency between the validation layer and the domain's instantiation rules.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the decimal property.</param>
    /// <returns>The rule builder conditions for further chaining.</returns>
    public static IRuleBuilderOptionsConditions<T, decimal> IsValidPrice<T>(
        this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            // Delegate the validation logic to the Value Object's factory method.
            // This maintains a "Single Source of Truth" for what constitutes a valid price.
            var result = Price.Create(value);

            if (result.IsError)
            {
                foreach (var error in result.Errors)
                {
                    // Map domain-level error descriptions directly to FluentValidation failures.
                    context.AddFailure(error.Description);
                }
            }
        });
    }

    /// <summary>
    /// Validates a string value using the domain's <see cref="Sku"/> (Stock Keeping Unit) Value Object logic.
    /// Includes pre-check optimizations for optional or empty fields.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the string property.</param>
    /// <returns>The rule builder conditions for further chaining.</returns>
    public static IRuleBuilderOptionsConditions<T, string> IsValidSku<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            // Early exit for null or whitespace to allow the 'NotEmpty' or 'Nullable' 
            // rules to handle basic presence, avoiding redundant domain logic calls.
            if (string.IsNullOrWhiteSpace(value)) return;

            var result = Sku.Create(value);

            if (result.IsError)
            {
                foreach (var error in result.Errors)
                {
                    // Propagate specific domain constraints (e.g., regex match, length) to the UI/API response.
                    context.AddFailure(error.Description);
                }
            }
        });
    }
}
