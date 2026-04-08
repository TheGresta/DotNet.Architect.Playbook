using Microsoft.Extensions.Caching.Hybrid;

using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

/// <summary>
/// A sealed implementation of <see cref="ICacheProvider"/> that orchestrates communication
/// between the <see cref="HybridCache"/>, <see cref="ICacheKeyProvider"/>, <see cref="ICacheTagFactory"/>,
/// and type-specific <see cref="ICachePolicy{T}"/>.
/// </summary>
internal sealed class CacheProvider(
    HybridCache cache,
    ICacheKeyProvider keyProvider,
    ICacheTagFactory tagFactory,
    IServiceProvider serviceProvider) : ICacheProvider
{
    /// <summary>
    /// Retrieves or creates a cache entry using a combination of local (L1) and distributed (L2) caching logic.
    /// </summary>
    /// <typeparam name="T">The type of the data being cached.</typeparam>
    /// <param name="factory">The asynchronous factory method to invoke on a cache miss.</param>
    /// <param name="identifier">The unique entity identifier for key generation.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The cached or refreshed object instance.</returns>
    public async Task<T> GetOrAddAsync<T>(
        Func<CancellationToken, ValueTask<T>> factory,
        string? identifier,
        CancellationToken ct) where T : class
    {
        // Resolve the specific policy for the requested type to determine expiration and prefixing rules.
        var policy = GetPolicy<T>();

        var key = keyProvider.GetKey(policy.Prefix, identifier);
        var tags = tagFactory.Build(policy.Prefix, policy.Tags);

        // Map policy-level bypass settings to the HybridCache's internal flag system.
        var flags = policy.BypassMemory ? HybridCacheEntryFlags.DisableLocalCache : HybridCacheEntryFlags.None;

        var options = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = policy.MemoryCacheExpiration,
            Expiration = policy.DistributedCacheExpiration,
            Flags = flags
        };

        // HybridCache handles 'stampede protection' internally, ensuring the factory only runs once for concurrent misses.
        return await cache.GetOrCreateAsync(key, factory, options, tags, ct);
    }

    /// <summary>
    /// Invalidates all cache entries associated with the tags defined in the policy for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type used to resolve the invalidation tags.</typeparam>
    /// <param name="ct">The cancellation token.</param>
    public async Task NotifyInvalidationAsync<T>(CancellationToken ct)
        where T : class
    {
        var policy = GetPolicy<T>();

        var tags = tagFactory.Build(policy.Prefix, policy.Tags);

        // Uses tag-based invalidation to clear multiple related keys (e.g., all products for a specific vendor).
        await cache.RemoveByTagAsync(tags, ct);
    }

    /// <summary>
    /// Dynamically resolves the cache policy associated with the generic type from the service container.
    /// </summary>
    /// <typeparam name="T">The type to resolve a policy for.</typeparam>
    /// <returns>An instance of <see cref="ICachePolicy{T}"/>.</returns>
    private ICachePolicy<T> GetPolicy<T>() where T : class
        => serviceProvider.GetRequiredService<ICachePolicy<T>>();
}