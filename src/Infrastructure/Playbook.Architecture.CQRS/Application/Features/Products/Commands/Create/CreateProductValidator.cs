using FluentValidation;

using Playbook.Architecture.CQRS.Application.Common.Extensions;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        // Clean, specialized extensions
        RuleFor(x => x.Price)
            .IsValidPrice();

        RuleFor(x => x.Sku)
            .NotEmpty()
            .IsValidSku()
            .When(x => x.Sku != null);
    }
}
