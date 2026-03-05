namespace Playbook.Persistence.Redis.Interfaces;

public interface ICacheService
{
    // Simple key-value operations (bypass versioning – use with care)
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    // Versioned prefix operations (recommended)
    ValueTask<T> GetOrSetAsync<T>(
        string prefix,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task InvalidatePrefixAsync(string prefix, CancellationToken cancellationToken = default);

    // Physical deletion by prefix (expensive, use sparingly)
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
