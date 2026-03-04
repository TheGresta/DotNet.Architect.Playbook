using Microsoft.Extensions.Caching.Memory;
using Playbook.Persistence.Redis.Application;
using Polly;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

public sealed class HybridCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _l1;
    private readonly IDatabase _l2;
    private readonly ISubscriber _subscriber;
    private readonly ICacheSerializer _serializer;
    private readonly ResiliencePipeline _resilience;
    private const string InvalidationChannel = "cache-invalidation-channel";

    public HybridCacheService(
        IMemoryCache l1,
        IConnectionMultiplexer redis,
        ICacheSerializer serializer,
        ResiliencePipeline resilience)
    {
        _l1 = l1;
        _l2 = redis.GetDatabase();
        _serializer = serializer;
        _resilience = resilience;
        _subscriber = redis.GetSubscriber();

        // Subscribe to invalidation messages from other instances
        _subscriber.Subscribe(InvalidationChannel, (channel, key) =>
        {
            _l1.Remove(key.ToString());
        });
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // 1. Try L1 (Memory) - Near zero latency
        if (_l1.TryGetValue(key, out T? localValue))
        {
            return localValue;
        }

        // 2. Try L2 (Redis) with Resilience
        var remoteValue = await _resilience.ExecuteAsync(async _ =>
        {
            var data = await _l2.StringGetAsync(key);
            return data.HasValue ? _serializer.Deserialize<T>(data!) : default;
        }, ct);

        // 3. If found in L2, populate L1 for next time
        if (remoteValue is not null)
        {
            _l1.Set(key, remoteValue, TimeSpan.FromMinutes(1)); // Short L1 TTL
        }

        return remoteValue;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? exp = null, CancellationToken ct = default)
    {
        if (value is null) return;

        // 1. Update L2 (Source of Truth for distributed state)
        await _resilience.ExecuteAsync(async _ =>
        {
            var bytes = _serializer.Serialize(value);
            await _l2.StringSetAsync(key, bytes, exp, When.Always);
        }, ct);

        // 2. Invalidate L1 locally and globally via Pub/Sub
        await _subscriber.PublishAsync(InvalidationChannel, key);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _l2.KeyDeleteAsync(key);
        await _subscriber.PublishAsync(InvalidationChannel, key);
    }

    public void Dispose()
    {
        _subscriber.Unsubscribe(InvalidationChannel);
    }

    // Note: GetOrSetAsync would follow the same pattern: Check L1 -> Check L2 -> Lock -> Factory
}
