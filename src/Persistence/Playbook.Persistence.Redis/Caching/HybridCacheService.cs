using System.Buffers;
using Microsoft.Extensions.Caching.Memory;
using Playbook.Persistence.Redis.Interfaces;
using Polly;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

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

    // Initialize subscription in a controlled manner
    private bool _isSubscribed;

    private void EnsureSubscribed()
    {
        if (_isSubscribed) return;
        _subscriber.Subscribe(InvalidationChannel, OnMessage);
        _isSubscribed = true;
    }

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        EnsureSubscribed();

        // Path 1: L1 Fast-Hit
        if (l1.TryGetValue(key, out T? localValue))
            return localValue;

        // Path 2: L2 with Resilience
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

        // Fast-path: Optimized L1 check
        if (l1.TryGetValue(versionedKey, out T? localValue))
            return localValue!;

        // Striped Locking to prevent Cache Stampede
        var lockIndex = (uint)versionedKey.GetHashCode() % (uint)_locks.Length;
        await _locks[lockIndex].WaitAsync(ct);

        try
        {
            // Double-check lock
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

            // Final fallback: Data Source
            var result = await factory(ct);
            await SetInternalAsync(versionedKey, result, expiration, ct);
            return result;
        }
        finally
        {
            _locks[lockIndex].Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken ct)
    {
        await SetInternalAsync(key, value, expiration, ct);
        await _subscriber.PublishAsync(InvalidationChannel, key, CommandFlags.FireAndForget);
    }

    public async Task InvalidatePrefixAsync(string prefix, CancellationToken ct)
    {
        var versionKey = $"{prefix}{VersionSuffix}";

        await resiliencePipeline.ExecuteAsync(
            async token => await _l2.StringIncrementAsync(versionKey), ct);

        l1.Remove(versionKey);
        await _subscriber.PublishAsync(InvalidationChannel, $"PURGE_VER:{prefix}", CommandFlags.FireAndForget);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await _l2.KeyDeleteAsync(key);
        l1.Remove(key);
        await _subscriber.PublishAsync(InvalidationChannel, key, CommandFlags.FireAndForget);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        // Lua script to scan and delete keys with the given prefix
        // This is expensive; consider using version invalidation instead.
        var script = @"
            local cursor = '0'
            repeat
                local res = redis.call('SCAN', cursor, 'MATCH', ARGV[1], 'COUNT', 100)
                cursor = res[1]
                for i, key in ipairs(res[2]) do redis.call('DEL', key) end
            until cursor == '0'";

        await _l2.ScriptEvaluateAsync(script, values: [$"{prefix}*"]);

        // No need to publish – version invalidation handles logical staleness.
        // Physical L1 cleanup is left to TTL.
    }

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

    private void OnMessage(RedisChannel channel, RedisValue message)
    {
        string? msg = message;
        if (string.IsNullOrEmpty(msg)) return;

        if (msg.StartsWith("PURGE_VER:"))
        {
            l1.Remove($"{msg[10..]}{VersionSuffix}"); // C# Range operator for "PURGE_VER:".Length
        }
        else
        {
            l1.Remove(msg);
        }
    }

    public void Dispose()
    {
        if (_isSubscribed)
        {
            _subscriber.Unsubscribe(InvalidationChannel, OnMessage);
        }

        foreach (var semaphore in _locks) semaphore.Dispose();
    }
}