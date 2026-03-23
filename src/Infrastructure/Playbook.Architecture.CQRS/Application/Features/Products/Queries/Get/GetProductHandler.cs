using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

public class GetProductHandler(IProductRepository repository)
    : IRequestHandler<GetProductQuery, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(
        GetProductQuery request,
        CancellationToken ct)
    {
        var result = await repository.GetByIdAsync(request.Id, ct);

        return result.Match(
            p => new ProductResponse(p.Id, p.Name, p.Price, p.Sku),
            errors => ErrorOr<ProductResponse>.From(errors)
        );
    }
}
