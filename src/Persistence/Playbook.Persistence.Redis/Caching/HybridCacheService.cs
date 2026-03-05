using System.Buffers;
using Microsoft.Extensions.Caching.Memory;
using Playbook.Persistence.Redis.Interfaces;
using Polly;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

public sealed class HybridCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _l1;
    private readonly IDatabase _l2;
    private readonly ISubscriber _subscriber;
    private readonly ICacheSerializer _serializer;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly int _lockCount;
    private readonly SemaphoreSlim[] _locks;
    private readonly TimeSpan _l1ShortTtl = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _l1VersionTtl = TimeSpan.FromMinutes(10);

    private const string InvalidationChannel = "cache:invalidation";
    private const string VersionSuffix = ":v";

    public HybridCacheService(
        IMemoryCache l1,
        IConnectionMultiplexer redis,
        ICacheSerializer serializer,
        [FromKeyedServices("redis-strategy")] ResiliencePipeline resiliencePipeline,
        int lockCount = 256) // Configurable lock count
    {
        _l1 = l1;
        _l2 = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _serializer = serializer;
        _resiliencePipeline = resiliencePipeline;
        _lockCount = lockCount;
        _locks = [.. Enumerable.Range(0, _lockCount).Select(_ => new SemaphoreSlim(1, 1))];

        // Subscribe to invalidation messages
        _subscriber.Subscribe(InvalidationChannel, OnMessage);
    }

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // L1 hit
        if (_l1.TryGetValue(key, out T? localValue))
            return localValue;

        // L2 hit with resilience
        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var data = await _l2.StringGetAsync(key);
            if (!data.HasValue) return default;

            ReadOnlyMemory<byte> memory = data;
            var value = _serializer.Deserialize<T>(memory.Span);
            if (value is null) return default;

            // Backfill L1 with a short TTL
            _l1.Set(key, value, _l1ShortTtl);
            return value;
        }, cancellationToken);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        await SetInternalAsync(key, value, expiration, cancellationToken);

        // Notify other instances to remove this key from their L1
        await _subscriber.PublishAsync(InvalidationChannel, key, CommandFlags.FireAndForget);
    }

    public async ValueTask<T> GetOrSetAsync<T>(
    string prefix,
    string key,
    Func<CancellationToken, Task<T>> factory,
    TimeSpan? expiration = null,
    CancellationToken cancellationToken = default)
    {
        var version = await GetPrefixVersionAsync(prefix, cancellationToken);
        var versionedKey = $"{prefix}:{version}:{key}";

        // Fast path: L1 hit
        if (_l1.TryGetValue(versionedKey, out T? localValue))
            return localValue!;

        var lockIndex = (uint)versionedKey.GetHashCode() % (uint)_lockCount;
        await _locks[lockIndex].WaitAsync(cancellationToken);
        try
        {
            // Double-check L1 after lock
            if (_l1.TryGetValue(versionedKey, out localValue))
                return localValue!;

            // Check L2
            var data = await _l2.StringGetAsync(versionedKey);
            if (data.HasValue)
            {
                ReadOnlyMemory<byte> memory = data;
                var value = _serializer.Deserialize<T>(memory.Span);
                if (value != null)
                {
                    _l1.Set(versionedKey, value, _l1ShortTtl);
                    return value;
                }
            }

            // Miss: execute factory and store
            var result = await factory(cancellationToken);
            await SetInternalAsync(versionedKey, result, expiration, cancellationToken);
            return result;
        }
        finally
        {
            _locks[lockIndex].Release();
        }
    }

    public async Task InvalidatePrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var versionKey = $"{prefix}{VersionSuffix}";

        // Atomically increment the version in Redis
        await _resiliencePipeline.ExecuteAsync(async ct =>
            await _l2.StringIncrementAsync(versionKey), cancellationToken);

        // Remove the local version cache entry
        _l1.Remove(versionKey);

        // Notify other instances to purge their version cache
        await _subscriber.PublishAsync(InvalidationChannel, $"PURGE_VER:{prefix}", CommandFlags.FireAndForget);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _l2.KeyDeleteAsync(key);
        _l1.Remove(key);
        await _subscriber.PublishAsync(InvalidationChannel, key, CommandFlags.FireAndForget);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
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

    private async Task SetInternalAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        if (value is null) return;

        var bufferWriter = new ArrayBufferWriter<byte>(256);
        _serializer.Serialize(bufferWriter, value);

        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            await _l2.StringSetAsync(
                key,
                bufferWriter.WrittenMemory,   // Implicitly converts to RedisValue
                expiration ?? TimeSpan.FromHours(24),
                When.Always);
        }, cancellationToken);

        // Backfill L1 with a short TTL
        _l1.Set(key, value, _l1ShortTtl);
    }

    private async ValueTask<long> GetPrefixVersionAsync(string prefix, CancellationToken cancellationToken)
    {
        var versionKey = $"{prefix}{VersionSuffix}";
        if (_l1.TryGetValue(versionKey, out long version))
            return version;

        var v = await _resiliencePipeline.ExecuteAsync(async ct =>
            await _l2.StringGetAsync(versionKey), cancellationToken);

        version = v.HasValue ? (long)v : 1L;
        _l1.Set(versionKey, version, _l1VersionTtl);
        return version;
    }

    private void OnMessage(RedisChannel channel, RedisValue message)
    {
        var msg = message.ToString();
        if (string.IsNullOrEmpty(msg)) return;

        if (msg.StartsWith("PURGE_VER:"))
        {
            // Remove version key from L1
            var prefix = msg.Replace("PURGE_VER:", "");
            _l1.Remove($"{prefix}{VersionSuffix}");
        }
        else
        {
            // Remove a specific key from L1
            _l1.Remove(msg);
        }
    }

    public void Dispose()
    {
        _subscriber.Unsubscribe(InvalidationChannel);
        foreach (var semaphore in _locks)
            semaphore.Dispose();
    }
}
