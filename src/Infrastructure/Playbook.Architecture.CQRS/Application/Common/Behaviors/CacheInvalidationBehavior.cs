using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeys { get; }
}

public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidator
    where TResponse : IErrorOr
{
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(2);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response.IsError)
        {
            return response;
        }

        try
        {
            // Aggregated task execution to minimize sequential overhead
            var invalidationTasks = request.CacheKeys.Select(async key =>
            {
                using var timeoutCts = new CancellationTokenSource(_cacheTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                // In a true Big Tech environment, you might push this
                // failed key to a "Background Retry Queue" (like Hangfire or RabbitMQ).
                await cache.RemoveAsync(key, linkedCts.Token);
                logger.LogInformation("Successfully invalidated: {CacheKey}", key);
            });

            await Task.WhenAll(invalidationTasks);
        }
        catch (Exception ex)
        {
            // Log once for the entire batch to reduce I/O and noise
            logger.LogError(ex, "Cache invalidation failure for keys: {Keys}",
                string.Join(", ", request.CacheKeys));
        }

        return response;
    }
}
