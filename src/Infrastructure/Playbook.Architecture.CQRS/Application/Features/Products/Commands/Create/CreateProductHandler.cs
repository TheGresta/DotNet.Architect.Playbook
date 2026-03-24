using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Extensions;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Domain.Entities;
using Playbook.Architecture.CQRS.Domain.ValueObjects;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

public class CreateProductHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(
    CreateProductCommand request,
    CancellationToken ct) =>
        await Price.Create(request.Price)
        .Then(price => Sku.Create(request.Sku).Map(sku => (price, sku)))
        .Then(data => CreateProduct(request.Name, data.price, data.sku))
        .ThenAsync(product => SaveProduct(product, ct))
        .MapAsync(ToResponse);

    // 3. Create Product (The Entity Constructor)
    private static ErrorOr<Product> CreateProduct(string name, Price price, Sku sku)
    {
        // You could add extra multi-property domain logic here
        return new Product(Guid.NewGuid(), name, price, sku);
    }

    // 4. Persist to Database
    private async Task<ErrorOr<Product>> SaveProduct(Product product, CancellationToken ct)
    {
        await repository.AddAsync(product, ct);
        return product;
    }

    private ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Price, product.Sku);
}
