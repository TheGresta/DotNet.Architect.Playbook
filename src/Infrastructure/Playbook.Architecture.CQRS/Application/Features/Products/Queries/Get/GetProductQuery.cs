using ErrorOr;

using Playbook.Architecture.CQRS.Application.Common.Behaviors;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

/// <summary>
/// Represents a read-only query to retrieve a specific product by its identifier.
/// Implements <see cref="IQuery{TResponse}"/> for architectural consistency and 
/// <see cref="ICachableQuery"/> to enable automated distributed caching.
/// </summary>
/// <param name="Id">The unique identifier of the product to fetch.</param>
public record GetProductQuery(Guid Id) : IQuery<ErrorOr<ProductResponse>>, ICachableQuery
{
    /// <summary>
    /// Gets the unique cache key derived from the product identity to ensure granular cache invalidation and retrieval.
    /// </summary>
    public string CacheKey => $"product-{Id}";

    /// <summary>
    /// Gets the Time-to-Live (TTL) for the cached product data, set to a short duration to balance performance and data freshness.
    /// </summary>
    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}
