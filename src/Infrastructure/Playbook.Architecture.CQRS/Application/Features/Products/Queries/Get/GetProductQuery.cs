using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Behaviors;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

public record GetProductQuery(Guid Id) : IRequest<ErrorOr<ProductResponse>>, ICachableQuery
{
    public string CacheKey => $"product-{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}
