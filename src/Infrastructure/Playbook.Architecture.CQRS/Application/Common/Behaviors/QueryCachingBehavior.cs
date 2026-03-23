using ErrorOr;

using MediatR;

using Microsoft.Extensions.Caching.Memory;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public interface ICachableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}

public class QueryCachingBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachableQuery
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (cache.TryGetValue(request.CacheKey, out TResponse? cachedResponse))
        {
            logger.LogInformation("--> Cache Hit for {CacheKey}", request.CacheKey);
            return cachedResponse!;
        }

        logger.LogInformation("--> Cache Miss for {CacheKey}. Fetching from source.", request.CacheKey);
        var response = await next(ct);

        if (!response.IsError)
        {
            cache.Set(request.CacheKey, response, request.Expiration ?? TimeSpan.FromMinutes(5));
        }

        return response;
    }
}
