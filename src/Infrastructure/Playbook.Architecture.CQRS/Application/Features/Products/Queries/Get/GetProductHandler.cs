using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Extensions;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Domain.Common;
using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

/// <summary>
/// The handler for <see cref="GetProductQuery"/> that coordinates data retrieval from the repository.
/// This handler is automatically wrapped by caching, logging, and exception behaviors in the MediatR pipeline.
/// </summary>
/// <param name="repository">The repository abstraction for product data access.</param>
public class GetProductHandler(IProductRepository repository)
    : IRequestHandler<GetProductQuery, ErrorOr<ProductResponse>>
{
    /// <summary>
    /// Executes the retrieval logic, transforming the repository result into a domain-aligned error or a response DTO.
    /// </summary>
    /// <param name="request">The query containing the target product ID.</param>
    /// <param name="ct">The cancellation token for the asynchronous operation.</param>
    /// <returns>A <see cref="ProductResponse"/> if found, or a <see cref="DomainErrors.Product.NotFound"/> error.</returns>
    public async Task<ErrorOr<ProductResponse>> Handle(
    GetProductQuery request,
    CancellationToken ct) =>
        // 1. Fetch the entity from the underlying data store.
        await repository.GetByIdAsync(request.Id, ct)
        // 2. Functional check to ensure the entity exists; otherwise, return a standardized 'Not Found' error.
        .EnsureFound(DomainErrors.Product.NotFound)
        // 3. Map the valid domain entity to a flat response DTO.
        .MapAsync(ToResponse);

    /// <summary>
    /// Projects the internal <see cref="Product"/> entity properties into a <see cref="ProductResponse"/>.
    /// Extracts raw values from Value Objects (Price and Sku) for external consumption.
    /// </summary>
    private static ProductResponse ToResponse(Product product) =>
        new(product.Id,
            product.Name,
            product.Price.Value,
            product.Sku.Value);
}
