using ErrorOr;

using FluentValidation;

using Playbook.Architecture.CQRS.Domain.ValueObjects;

namespace Playbook.Architecture.CQRS.Application.Common.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilderOptionsConditions<T, decimal> IsValidPrice<T>(this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = Price.Create(value);

            if (result.IsError)
            {
                foreach (var error in result.Errors)
                {
                    // Senior Tip: PropertyPath ensures the error aligns with the JSON key
                    context.AddFailure(context.PropertyPath, error.Description);
                }
            }
        });
    }

    public static IRuleBuilderOptionsConditions<T, string> IsValidSku<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            // We can handle null/empty checks here or let the VO handle it
            if (string.IsNullOrWhiteSpace(value)) return;

            var result = Sku.Create(value);

            if (result.IsError)
            {
                foreach (var error in result.Errors)
                {
                    context.AddFailure(context.PropertyPath, error.Description);
                }
            }
        });
    }
}
