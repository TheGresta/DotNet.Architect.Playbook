using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Extensions;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Domain.Entities;
using Playbook.Architecture.CQRS.Domain.ValueObjects;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

/// <summary>
/// The orchestrator for the <see cref="CreateProductCommand"/>. 
/// It utilizes a functional pipeline approach to validate domain invariants, compose the aggregate, 
/// and persist changes via the repository pattern.
/// </summary>
/// <param name="repository">The abstraction for product persistence.</param>
public class CreateProductHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, ErrorOr<ProductResponse>>
{
    /// <summary>
    /// Handles the product creation lifecycle using a fluent, monadic flow to ensure atomicity and clear error propagation.
    /// </summary>
    /// <param name="request">The command containing product details.</param>
    /// <param name="ct">The cancellation token for the asynchronous operation.</param>
    /// <returns>A successful <see cref="ProductResponse"/> or an <see cref="ErrorOr"/> containing domain/validation failures.</returns>
    public async Task<ErrorOr<ProductResponse>> Handle(
    CreateProductCommand request,
    CancellationToken ct) =>
        // 1. Initialize the flow by attempting to create a Price Value Object.
        await Price.Create(request.Price)
        // 2. Chain the Sku creation, merging both into a tuple if successful.
        .Then(price => Sku.Create(request.Sku).Map(sku => (price, sku)))
        // 3. Invoke the domain factory method to instantiate the Product entity.
        .Then(data => CreateProduct(request.Name, data.price, data.sku))
        // 4. Persist the valid aggregate to the data store.
        .ThenAsync(product => SaveProduct(product, ct))
        // 5. Project the internal entity to a public-facing DTO.
        .MapAsync(ToResponse);

    /// <summary>
    /// A domain factory method that encapsulates the instantiation logic for a <see cref="Product"/>.
    /// This acts as a secondary guard for multi-property invariants that transcend individual Value Objects.
    /// </summary>
    private static ErrorOr<Product> CreateProduct(string name, Price price, Sku sku)
    {
        // Internal entity creation with a newly generated Identity.
        return new Product(Guid.NewGuid(), name, price, sku);
    }

    /// <summary>
    /// Persists the validated product aggregate to the repository.
    /// </summary>
    private async Task<ErrorOr<Product>> SaveProduct(Product product, CancellationToken ct)
    {
        await repository.AddAsync(product, ct);
        return product;
    }

    /// <summary>
    /// Maps the domain entity to a flat Response DTO to decouple the internal model from the API contract.
    /// </summary>
    private ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Price, product.Sku);
}
