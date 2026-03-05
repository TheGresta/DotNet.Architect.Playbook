namespace Playbook.Persistence.Redis.Interfaces;

public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    ValueTask<T> GetOrSetAsync<T>(
        string prefix,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken ct = default);

    Task InvalidatePrefixAsync(string prefix, CancellationToken ct = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
