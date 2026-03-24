using System.Text.Json;

using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public interface ICachableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
    bool BypassCache => false;
}

public class QueryCachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, ICachableQuery
    where TResponse : IErrorOr
{
    // Semaphore prevents multiple threads from fetching the same data simultaneously
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request.BypassCache) return await next(ct);

        // 1. Try Get from Cache
        var cachedResponse = await GetFromCache(request.CacheKey, ct);
        if (cachedResponse is not null)
        {
            logger.LogInformation("--> Cache Hit: {CacheKey}", request.CacheKey);
            return cachedResponse;
        }

        // 2. Cache Miss - Lock to prevent Thundering Herd
        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check pattern: another thread might have filled the cache while we waited
            cachedResponse = await GetFromCache(request.CacheKey, ct);
            if (cachedResponse is not null) return cachedResponse;

            logger.LogInformation("--> Cache Miss: {CacheKey}. Fetching from Source.", request.CacheKey);

            var response = await next(ct);

            if (!response.IsError)
            {
                await SetInCache(request.CacheKey, response, request.Expiration, ct);
            }

            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<TResponse?> GetFromCache(string key, CancellationToken ct)
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null) return default;

        // Use a high-performance serializer like MessagePack or System.Text.Json
        return JsonSerializer.Deserialize<TResponse>(bytes);
    }

    private async Task SetInCache(string key, TResponse response, TimeSpan? expiration, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(response);
        await cache.SetAsync(key, bytes, options, ct);
    }
}
