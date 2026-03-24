using System.Collections.Concurrent;
using System.Text.Json;

using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

/// <summary>
/// Defines a contract for MediatR queries that support distributed caching.
/// Implementing this interface allows the pipeline to intercept the request and serve cached data when available.
/// </summary>
public interface ICachableQuery
{
    /// <summary>
    /// Gets the unique identifier used to store and retrieve the query result from the cache.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the duration for which the query result should remain in the cache. 
    /// If null, a system-wide default is applied.
    /// </summary>
    TimeSpan? Expiration { get; }

    /// <summary>
    /// Gets a value indicating whether the cache should be ignored, forcing a fresh fetch from the data source.
    /// Defaults to <c>false</c>.
    /// </summary>
    bool BypassCache => false;
}

/// <summary>
/// A high-performance MediatR pipeline behavior that implements the Cache-Aside pattern with Thundering Herd protection.
/// It utilizes a double-check locking mechanism via <see cref="SemaphoreSlim"/> to ensure that concurrent identical requests 
/// do not overwhelm the underlying data source during a cache miss.
/// </summary>
/// <typeparam name="TRequest">The type of the query, constrained to <see cref="ICachableQuery"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/>.</typeparam>
/// <param name="cache">The distributed cache provider (e.g., Redis or NCache).</param>
/// <param name="logger">The logger for tracking cache hits, misses, and synchronization events.</param>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachableQuery
    where TResponse : IErrorOr
{
    /// <summary>
    /// A thread-safe dictionary managing granular locks. 
    /// Mapping semaphores to specific <see cref="ICachableQuery.CacheKey"/> values ensures that 
    /// synchronization only occurs for identical requests, maintaining high throughput for distinct keys.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _lockGroups = new();

    /// <summary>
    /// The fallback expiration period applied if the request does not specify a custom <see cref="ICachableQuery.Expiration"/>.
    /// </summary>
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Orchestrates the caching lifecycle: Check, Lock, Fetch, and Populate.
    /// </summary>
    /// <param name="request">The incoming cachable query.</param>
    /// <param name="next">The execution delegate for the underlying handler.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation.</param>
    /// <returns>A <typeparamref name="TResponse"/> sourced from either the cache or the primary data provider.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Short-circuit the behavior if the consumer explicitly requests fresh data.
        if (request.BypassCache) return await next(cancellationToken);

        // 1. Initial optimistic check to maximize performance under high-read volume.
        var cachedResponse = await GetFromCache(request.CacheKey, cancellationToken);
        if (cachedResponse is not null)
        {
            logger.LogDebug("Cache Hit: {CacheKey}", request.CacheKey);
            return cachedResponse;
        }

        // 2. Obtain or create a semaphore specific to this CacheKey. 
        // This prevents the "Thundering Herd" or "Cache Stampede" effect where multiple 
        // concurrent threads attempt to refresh the same expired key simultaneously.
        var semaphore = _lockGroups.GetOrAdd(request.CacheKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // 3. Double-check pattern: Another thread might have populated the cache while this thread was waiting for the lock.
            cachedResponse = await GetFromCache(request.CacheKey, cancellationToken);
            if (cachedResponse is not null) return cachedResponse;

            logger.LogInformation("Cache Miss: {CacheKey}. Fetching from Source.", request.CacheKey);
            var response = await next();

            // Only persist successful responses to the cache to avoid poisoning it with temporary errors.
            if (!response.IsError)
            {
                await SetInCache(request.CacheKey, response, request.Expiration, cancellationToken);
            }

            return response;
        }
        finally
        {
            semaphore.Release();
            // Note: In extremely long-running processes with millions of unique keys, 
            // a background cleanup task for _lockGroups would be recommended to prevent memory growth.
        }
    }

    /// <summary>
    /// Retrieves and deserializes data from the distributed cache.
    /// </summary>
    private async Task<TResponse?> GetFromCache(string key, CancellationToken ct)
    {
        var bytes = await cache.GetAsync(key, ct);
        // Using System.Text.Json for high-performance, low-allocation deserialization.
        return bytes is null ? default : JsonSerializer.Deserialize<TResponse>(bytes);
    }

    /// <summary>
    /// Serializes and stores the successful response in the distributed cache with specified TTL.
    /// </summary>
    private async Task SetInCache(string key, TResponse response, TimeSpan? expiration, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        // Convert the response to a UTF-8 byte array to ensure compatibility with all IDistributedCache implementations.
        await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(response), options, ct);
    }
}
