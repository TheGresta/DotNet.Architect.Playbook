using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Extensions;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Domain.Common;
using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

public class GetProductHandler(IProductRepository repository)
    : IRequestHandler<GetProductQuery, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(
    GetProductQuery request,
    CancellationToken ct) =>
        await repository.GetByIdAsync(request.Id, ct)
        .EnsureFound(DomainErrors.Product.NotFound) // "Make sure we got something"
        .MapAsync(ToResponse);

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id,
            product.Name,
            product.Price.Value,
            product.Sku.Value);
}
