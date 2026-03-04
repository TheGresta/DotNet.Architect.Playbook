using System.Collections.Concurrent;
using Playbook.Persistence.Redis.Application;
using Polly;
using Polly.Registry;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    ICacheSerializer serializer,
    ResiliencePipelineProvider<string> pipelineProvider) : ICacheService
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ResiliencePipeline _resilience = pipelineProvider.GetPipeline("redis-strategy");

    // Optimized Locking: Striped Locking pattern to prevent memory leaks
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _lockPool = new();
    private const int LockPoolSize = 128; // Adjust based on expected concurrency

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Fast Path: Check L2
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue is not null) return cachedValue;

        // 2. Identify a bucket for this key (Striped Locking)
        var lockIndex = Math.Abs(key.GetHashCode() % LockPoolSize);
        var semaphore = _lockPool.GetOrAdd(lockIndex, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // 3. Double-Check Pattern
            cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue is not null) return cachedValue;

            // 4. Cache Miss: Execute Factory
            var result = await factory(cancellationToken);

            // 5. Store result
            if (result is not null)
            {
                await SetAsync(key, result, expiration, cancellationToken);
            }

            return result!;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _resilience.ExecuteAsync(async token =>
        {
            var data = await _database.StringGetAsync(key);
            return data.HasValue ? serializer.Deserialize<T>(data!) : default;
        }, cancellationToken);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null) return;

        await _resilience.ExecuteAsync(async token =>
        {
            var bytes = serializer.Serialize(value);
            await _database.StringSetAsync(key, bytes, expiration, When.Always);
        }, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Architect Note: Key scanning is O(N). Use with caution in high-load envs.
        // Better: Use Redis Sets to track keys by category.
        var endPoints = redis.GetEndPoints();
        foreach (var endpoint in endPoints)
        {
            var server = redis.GetServer(endpoint);
            // Using '*' as a wildcard for prefix matching
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            await _database.KeyDeleteAsync(keys);
        }
    }
}