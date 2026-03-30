using Microsoft.Extensions.Caching.Hybrid;

using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

internal sealed class CacheProvider(HybridCache cache, ICacheKeyProvider keyProvider, IServiceProvider serviceProvider) : ICacheProvider
{
    public async Task<T> GetOrAddAsync<T>(
        Func<CancellationToken, ValueTask<T>> factory,
        string? identifier,
        CancellationToken ct) where T : class
    {
        var policy = GetPolicy<T>();

        var key = keyProvider.GetKey(policy.Prefix, identifier);
        var tags = keyProvider.GetTags(policy.Prefix, policy.VendorIds);
        var flags = policy.BypassMemory ? HybridCacheEntryFlags.DisableLocalCache : HybridCacheEntryFlags.None;

        var options = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = policy.MemoryCacheExpiration,
            Expiration = policy.DistributedCacheExpiration,
            Flags = flags
        };

        return await cache.GetOrCreateAsync(key, factory, options, tags, ct);
    }

    public async Task NotifyInvalidationAsync<T>(CancellationToken ct)
        where T : class
    {
        var policy = GetPolicy<T>();

        var tags = keyProvider.GetTags(policy.Prefix, policy.VendorIds);

        await cache.RemoveByTagAsync(tags, ct);
    }
    private ICachePolicy<T> GetPolicy<T>() where T : class
        => serviceProvider.GetRequiredService<ICachePolicy<T>>();
}
