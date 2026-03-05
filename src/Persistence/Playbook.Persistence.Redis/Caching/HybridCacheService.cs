using System.Buffers;
using Microsoft.Extensions.Caching.Memory;
using Playbook.Persistence.Redis.Interfaces;
using Polly;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

/// <summary>
/// A high-performance, resilient multi-level cache service orchestrating <see cref="IMemoryCache"/> (L1) 
/// and Redis (L2) with distributed invalidation.
/// </summary>
/// <remarks>
/// This service implements:
/// <list type="bullet">
/// <item><description><b>Striped Locking:</b> Reduces lock contention during cache stampedes using an array of <see cref="SemaphoreSlim"/>.</description></item>
/// <item><description><b>Hybrid Invalidation:</b> Uses Redis Pub/Sub to synchronize L1 cache eviction across multiple application instances.</description></item>
/// <item><description><b>Versioned Keys:</b> Supports logical prefix invalidation by incrementing a version counter in Redis.</description></item>
/// </list>
/// </remarks>
public sealed class HybridCacheService(
    IMemoryCache l1,
    IConnectionMultiplexer redis,
    ICacheSerializer serializer,
    [FromKeyedServices("redis-strategy")] ResiliencePipeline resiliencePipeline,
    int lockCount = 256) : ICacheService, IDisposable
{
    private readonly IDatabase _l2 = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    private readonly SemaphoreSlim[] _locks = [.. Enumerable.Range(0, lockCount).Select(_ => new SemaphoreSlim(1, 1))];

    private readonly TimeSpan _l1ShortTtl = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _l1VersionTtl = TimeSpan.FromMinutes(10);

    private const string InvalidationChannel = "cache:invalidation";
    private const string VersionSuffix = ":v";

    private bool _isSubscribed;

    /// <summary>
    /// Ensures the service is subscribed to the Redis invalidation channel.
    /// </summary>
    private void EnsureSubscribed()
    {
        if (_isSubscribed) return;
        _subscriber.Subscribe(InvalidationChannel, OnMessage);
        _isSubscribed = true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Checks the L1 cache first. If a miss occurs, it executes the <see cref="ResiliencePipeline"/> 
    /// to fetch data from Redis and populates the L1 cache upon success.
    /// </remarks>
    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        EnsureSubscribed();

        if (l1.TryGetValue(key, out T? localValue))
            return localValue;

        return await resiliencePipeline.ExecuteAsync(async token =>
        {
            var data = await _l2.StringGetAsync(key);
            if (!data.HasValue) return default;

            byte[]? bytes = data;
            var value = serializer.Deserialize<T>(bytes.AsSpan());
            if (value is null) return default;

            l1.Set(key, value, _l1ShortTtl);
            return value;
        }, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Utilizes striped locking based on the hash of the versioned key to prevent multiple concurrent 
    /// factory executions for the same resource. 
    /// <para/>
    /// The key is composed as <c>{prefix}:{version}:{key}</c> to support instantaneous prefix invalidation.
    /// </remarks>
    public async ValueTask<T> GetOrSetAsync<T>(
        string prefix,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken ct)
    {
        EnsureSubscribed();

        long version = await GetPrefixVersionAsync(prefix, ct);
        string versionedKey = $"{prefix}:{version}:{key}";

        if (l1.TryGetValue(versionedKey, out T? localValue))
            return localValue!;

        var lockIndex = (uint)versionedKey.GetHashCode() % (uint)_locks.Length;
        await _locks[lockIndex].WaitAsync(ct);

        try
        {
            if (l1.TryGetValue(versionedKey, out localValue))
                return localValue!;

            var data = await _l2.StringGetAsync(versionedKey);
            if (data.HasValue)
            {
                byte[]? bytes = data;
                var value = serializer.Deserialize<T>(bytes.AsSpan());
                if (value is not null)
                {
                    l1.Set(versionedKey, value, _l1ShortTtl);
                    return value;
                }
            }

            var result = await factory(ct);
            await SetInternalAsync(versionedKey, result, expiration, ct);
            return result;
        }
        finally
        {
            _locks[lockIndex].Release();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Persists the value to Redis and L1, then broadcasts an invalidation message via Redis Pub/Sub 
    /// to notify other instances to evict the key from their local L1 caches.
    /// </remarks>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken ct)
    {
        await SetInternalAsync(key, value, expiration, ct);
        await _subscriber.PublishAsync(InvalidationChannel, key, CommandFlags.FireAndForget);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Atomically increments the prefix version in Redis and clears the local version cache. 
    /// This effectively invalidates all keys under this prefix across the distributed system.
    /// </remarks>
    public async Task InvalidatePrefixAsync(string prefix, CancellationToken ct)
    {
        var versionKey = $"{prefix}{VersionSuffix}";

        await resiliencePipeline.ExecuteAsync(
            async token => await _l2.StringIncrementAsync(versionKey), ct);

        l1.Remove(versionKey);
        await _subscriber.PublishAsync(InvalidationChannel, $"PURGE_VER:{prefix}", CommandFlags.FireAndForget);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await _l2.KeyDeleteAsync(key);
        l1.Remove(key);
        await _subscriber.PublishAsync(InvalidationChannel, key, CommandFlags.FireAndForget);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Executes a Lua script to perform a <c>SCAN</c> and <c>DEL</c> operation in Redis.
    /// <para>Warning: This is an O(N) operation and should be used sparingly for large datasets. 
    /// Prefer <see cref="InvalidatePrefixAsync"/> for high-frequency invalidation.</para>
    /// </remarks>
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        var script = @"
            local cursor = '0'
            repeat
                local res = redis.call('SCAN', cursor, 'MATCH', ARGV[1], 'COUNT', 100)
                cursor = res[1]
                for i, key in ipairs(res[2]) do redis.call('DEL', key) end
            until cursor == '0'";

        await _l2.ScriptEvaluateAsync(script, values: [$"{prefix}*"]);
    }

    /// <summary>
    /// Internal helper to serialize and store data in both cache levels.
    /// </summary>
    private async Task SetInternalAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken ct)
    {
        if (value is null) return;

        var bufferWriter = new ArrayBufferWriter<byte>(256);
        serializer.Serialize(bufferWriter, value);

        await resiliencePipeline.ExecuteAsync(async token =>
        {
            await _l2.StringSetAsync(
                key,
                bufferWriter.WrittenMemory,
                expiration ?? TimeSpan.FromHours(24));
        }, ct);

        l1.Set(key, value, _l1ShortTtl);
    }

    /// <summary>
    /// Retrieves the current version number for a prefix from L1 or L2.
    /// </summary>
    private async ValueTask<long> GetPrefixVersionAsync(string prefix, CancellationToken ct)
    {
        var versionKey = $"{prefix}{VersionSuffix}";
        if (l1.TryGetValue(versionKey, out long version))
            return version;

        var v = await resiliencePipeline.ExecuteAsync(
            async token => await _l2.StringGetAsync(versionKey), ct);

        version = v.HasValue ? (long)v : 1L;
        l1.Set(versionKey, version, _l1VersionTtl);
        return version;
    }

    /// <summary>
    /// Handles incoming Redis Pub/Sub messages to synchronize L1 eviction.
    /// </summary>
    private void OnMessage(RedisChannel channel, RedisValue message)
    {
        string? msg = message;
        if (string.IsNullOrEmpty(msg)) return;

        if (msg.StartsWith("PURGE_VER:"))
        {
            l1.Remove($"{msg[10..]}{VersionSuffix}");
        }
        else
        {
            l1.Remove(msg);
        }
    }

    /// <summary>
    /// Releases the resources used by the <see cref="HybridCacheService"/>, 
    /// including semaphores and Redis subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_isSubscribed)
        {
            _subscriber.Unsubscribe(InvalidationChannel, OnMessage);
        }

        foreach (var semaphore in _locks) semaphore.Dispose();
    }
}