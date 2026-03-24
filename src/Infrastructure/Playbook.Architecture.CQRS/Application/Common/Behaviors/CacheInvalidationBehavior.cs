using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public interface ICacheInvalidator
{
    // A command might invalidate multiple keys (e.g., 'product-1' and 'all-products')
    IEnumerable<string> CacheKeys { get; }
}

public class CacheInvalidationBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidator
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken ct)
    {
        // 1. Execute the Handler first (The source of truth)
        var response = await next(ct);

        // 2. Only attempt invalidation if the DB work succeeded
        if (!response.IsError)
        {
            try
            {
                foreach (var key in request.CacheKeys)
                {
                    // We use a shorter timeout for cache ops so they don't hang the request
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

                    await cache.RemoveAsync(key, linkedCts.Token);
                    logger.LogInformation("--> Successfully invalidated: {CacheKey}", key);
                }
            }
            catch (Exception ex)
            {
                // CRITICAL: We log the error but we DO NOT throw.
                // The user's data is safe in the DB; we just have a "Stale Cache" risk now.
                logger.LogError(ex, "--> Failed to invalidate cache. System will have stale data for {Keys}",
                    string.Join(", ", request.CacheKeys));

                // Optional: In a true Big Tech environment, you might push this 
                // failed key to a "Background Retry Queue" (like Hangfire or RabbitMQ).
            }
        }

        return response;
    }
}
