using FluentValidation;

using Playbook.Architecture.CQRS.Application.Common.Extensions;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

/// <summary>
/// A robust validator for <see cref="CreateProductCommand"/> that combines standard FluentValidation 
/// rules with specialized domain-driven extensions.
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Defines the validation surface for the command, ensuring basic data integrity before domain logic is invoked.
    /// </summary>
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        // Leverages custom extensions to check domain invariants (Price/Sku) early in the pipeline.
        RuleFor(x => x.Price)
            .IsValidPrice();

        RuleFor(x => x.Sku)
            .NotEmpty()
            .IsValidSku();
    }
}
