using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

public class CreateProductHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        var product = new Product(
            Guid.NewGuid(),
            request.Name,
            request.Price,
            request.Sku);

        var result = await repository.AddAsync(product, ct);

        // Functional Mapping: If success, map to DTO. If error, return error.
        return result.Match(
            _ => new ProductResponse(product.Id, product.Name, product.Price, product.Sku),
            errors => ErrorOr<ProductResponse>.From(errors)
        );
    }
}
