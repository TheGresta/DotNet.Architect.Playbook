namespace Playbook.Persistence.Redis.Interfaces;

/// <summary>
/// Defines a multi-level (L1/L2) caching service that provides synchronized access to local memory and distributed storage.
/// </summary>
/// <remarks>
/// This service typically manages an L1 cache (e.g., <c>MemoryCache</c>) for low-latency access and an L2 cache 
/// (e.g., Redis) for persistence and cross-instance consistency.
/// </remarks>
public interface ICacheService
{
    /// <summary>
    /// Asynchronously retrieves a cached value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The unique identifier for the cached item.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the value if found; 
    /// otherwise, <see langword="default"/>.
    /// </returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously stores a value in the cache with an optional expiration timeout.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The unique identifier for the cached item.</param>
    /// <param name="value">The data to be persisted in both L1 and L2 layers.</param>
    /// <param name="absoluteExpiration">An optional <see cref="TimeSpan"/> representing the fixed point in time when the cache entry should expire.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous set operation.</returns>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Asynchronously removes the value associated with the specified key from all cache levels.
    /// </summary>
    /// <param name="key">The unique identifier for the item to remove.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous removal operation.</returns>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Attempts to retrieve a value from the cache; if not found, executes the factory, stores the result, and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve or create.</typeparam>
    /// <param name="prefix">A grouping prefix used for logical isolation and mass invalidation.</param>
    /// <param name="key">The unique identifier for the cached item within the prefix.</param>
    /// <param name="factory">A delegate to retrieve the data from the underlying source if the cache is empty.</param>
    /// <param name="absoluteExpiration">An optional <see cref="TimeSpan"/> for the entry's lifetime.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The cached or freshly retrieved value of type <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// <para>
    /// Implementations should employ a locking mechanism (such as a <c>SemaphoreSlim</c>) based on the key to 
    /// prevent "Cache Stampede" where multiple threads call the <paramref name="factory"/> simultaneously.
    /// </para>
    /// <code language="csharp">
    /// var product = await cache.GetOrSetAsync("catalog", "p-123", ct => _db.GetProductAsync(123));
    /// </code>
    /// </remarks>
    ValueTask<T> GetOrSetAsync<T>(
        string prefix,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Asynchronously invalidates all cache entries associated with a specific prefix.
    /// </summary>
    /// <param name="prefix">The prefix identifying the group of items to invalidate.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invalidation.</returns>
    /// <remarks>
    /// This is often used for "soft" invalidation, where the L1 cache is cleared and the L2 cache is marked as stale.
    /// </remarks>
    Task InvalidatePrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously removes all cache entries that match the specified prefix from all levels.
    /// </summary>
    /// <param name="prefix">The prefix identifying the group of items to delete.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous removal operation.</returns>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
