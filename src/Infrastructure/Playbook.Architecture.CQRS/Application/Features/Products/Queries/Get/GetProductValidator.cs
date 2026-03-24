using FluentValidation;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

/// <summary>
/// Provides specialized validation for the <see cref="GetProductQuery"/> to ensure data integrity before query execution.
/// </summary>
public class GetProductValidator : AbstractValidator<GetProductQuery>
{
    /// <summary>
    /// Initializes validation rules for the product retrieval query.
    /// </summary>
    public GetProductValidator()
    {
        // Ensure the Guid is not empty (00000000-0000-0000-0000-000000000000).
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
