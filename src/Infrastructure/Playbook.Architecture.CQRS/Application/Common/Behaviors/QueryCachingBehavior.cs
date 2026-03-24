using System.Collections.Concurrent;
using System.Text.Json;

using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public interface ICachableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
    bool BypassCache => false;
}

public sealed class QueryCachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachableQuery
    where TResponse : IErrorOr
{
    // High-performance concurrency management: One lock per unique CacheKey
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _lockGroups = new();
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request.BypassCache) return await next(cancellationToken);

        // 1. Optimized L1/L2 Check
        var cachedResponse = await GetFromCache(request.CacheKey, cancellationToken);
        if (cachedResponse is not null)
        {
            logger.LogDebug("Cache Hit: {CacheKey}", request.CacheKey);
            return cachedResponse;
        }

        // 2. Specific Semaphore to prevent global thundering herd
        var semaphore = _lockGroups.GetOrAdd(request.CacheKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Double-check pattern
            cachedResponse = await GetFromCache(request.CacheKey, cancellationToken);
            if (cachedResponse is not null) return cachedResponse;

            logger.LogInformation("Cache Miss: {CacheKey}. Fetching from Source.", request.CacheKey);
            var response = await next();

            if (!response.IsError)
            {
                await SetInCache(request.CacheKey, response, request.Expiration, cancellationToken);
            }

            return response;
        }
        finally
        {
            semaphore.Release();
            // Optional: Periodic cleanup of _lockGroups to prevent memory leak
        }
    }

    private async Task<TResponse?> GetFromCache(string key, CancellationToken ct)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<TResponse>(bytes);
    }

    private async Task SetInCache(string key, TResponse response, TimeSpan? expiration, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(response), options, ct);
    }
}
