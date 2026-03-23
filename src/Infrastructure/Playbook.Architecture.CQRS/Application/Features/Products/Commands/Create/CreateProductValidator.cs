using FluentValidation;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Sku).NotEmpty().Matches(@"^[A-Z0-9-]*$")
            .WithMessage("SKU must be uppercase alphanumeric.");
    }
}
