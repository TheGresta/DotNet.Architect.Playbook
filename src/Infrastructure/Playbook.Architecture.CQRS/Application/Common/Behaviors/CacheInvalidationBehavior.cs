using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

/// <summary>
/// Defines a contract for requests that require cache invalidation logic.
/// Classes implementing this interface provide a collection of cache keys to be purged upon successful operation.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Gets the collection of unique identifiers (keys) representing the cached entries to be invalidated.
    /// </summary>
    IEnumerable<string> CacheKeys { get; }
}

/// <summary>
/// A MediatR pipeline behavior that automatically handles cache invalidation for requests implementing <see cref="ICacheInvalidator"/>.
/// This behavior ensures that cache consistency is maintained by purging relevant keys only after a successful request execution.
/// </summary>
/// <typeparam name="TRequest">The type of the request, constrained to <see cref="ICacheInvalidator"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/> to ensure error-aware invalidation.</typeparam>
/// <param name="cache">The distributed cache instance used for key removal.</param>
/// <param name="logger">The logger used for telemetry and error tracking during the invalidation process.</param>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidator
    where TResponse : IErrorOr
{
    /// <summary>
    /// The maximum allowable duration for an individual cache removal operation to prevent pipeline stalling.
    /// </summary>
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Handles the command/query execution and triggers cache invalidation if the inner handler succeeds.
    /// </summary>
    /// <param name="request">The incoming request containing cache keys to invalidate.</param>
    /// <param name="next">The delegate representing the next action in the MediatR pipeline.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The response from the subsequent handler in the pipeline.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        // Post-processor logic: Only invalidate cache if the operation was successful.
        // This prevents data inconsistency where a cache is cleared but the underlying state remains unchanged due to an error.
        if (response.IsError)
        {
            return response;
        }

        try
        {
            // Aggregated task execution to minimize sequential overhead. 
            // Leveraging LINQ to project cache keys into a set of concurrent Tasks.
            var invalidationTasks = request.CacheKeys.Select(async key =>
            {
                // Enforce a strict timeout per-key to prevent a hanging cache provider from blocking the entire request pipeline.
                using var timeoutCts = new CancellationTokenSource(_cacheTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                // In a true Big Tech environment, you might push this
                // failed key to a "Background Retry Queue" (like Hangfire or RabbitMQ).
                await cache.RemoveAsync(key, linkedCts.Token);
                logger.LogInformation("Successfully invalidated: {CacheKey}", key);
            });

            // Execute all invalidation tasks in parallel to improve throughput.
            await Task.WhenAll(invalidationTasks);
        }
        catch (Exception ex)
        {
            // Log once for the entire batch to reduce I/O and noise.
            // Failure to invalidate cache is treated as a non-breaking fault for the user, but a critical warning for system consistency.
            logger.LogError(ex, "Cache invalidation failure for keys: {Keys}",
                string.Join(", ", request.CacheKeys));
        }

        return response;
    }
}
