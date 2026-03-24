using FluentValidation;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

public class GetProductValidator : AbstractValidator<GetProductQuery>
{
    public GetProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
