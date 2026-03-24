using ErrorOr;

using Playbook.Architecture.CQRS.Application.Common.Behaviors;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

public record GetProductQuery(Guid Id) : IQuery<ErrorOr<ProductResponse>>, ICachableQuery
{
    public string CacheKey => $"product-{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}
