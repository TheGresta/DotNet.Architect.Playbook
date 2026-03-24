using FluentValidation;

using Playbook.Architecture.CQRS.Domain.ValueObjects;

namespace Playbook.Architecture.CQRS.Application.Common.Extensions;

public static class ValidationExtensions
{
    /// <summary>
    /// Validates a decimal value using the Price Value Object logic.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, decimal> IsValidPrice<T>(
        this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = Price.Create(value);

            if (result.IsError)
            {
                foreach (var error in result.Errors)
                {
                    context.AddFailure(error.Description);
                }
            }
        });
    }

    /// <summary>
    /// Validates a string value using the SKU Value Object logic.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, string> IsValidSku<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            // Early exit for optional fields to avoid redundant VO logic
            if (string.IsNullOrWhiteSpace(value)) return;

            var result = Sku.Create(value);

            if (result.IsError)
            {
                foreach (var error in result.Errors)
                {
                    context.AddFailure(error.Description);
                }
            }
        });
    }
}
