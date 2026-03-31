namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

/// <summary>
/// Defines the high-level contract for interacting with the hybrid caching layer, 
/// supporting atomic get-or-add operations and tag-based invalidation.
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// Attempts to retrieve an item from the cache. If the item is missing, executes the provided factory 
    /// to generate the value, stores it, and returns it.
    /// </summary>
    /// <typeparam name="T">The reference type of the cached data.</typeparam>
    /// <param name="factory">A delegate to produce the value if a cache miss occurs.</param>
    /// <param name="identifier">An optional unique identifier to differentiate entries within the same type category.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the cached or newly created value.</returns>
    Task<T> GetOrAddAsync<T>(
        Func<CancellationToken, ValueTask<T>> factory,
        string? identifier = null,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Triggers a mass invalidation of cached entries for the specified type <typeparamref name="T"/> 
    /// based on defined cache tags.
    /// </summary>
    /// <typeparam name="T">The type whose associated tags should be invalidated.</typeparam>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous invalidation operation.</returns>
    Task NotifyInvalidationAsync<T>(CancellationToken ct) where T : class;
}
