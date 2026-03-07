using Microsoft.AspNetCore.Mvc;

using Playbook.Persistence.Redis.Interfaces;

namespace Playbook.Persistence.Redis.Controllers;

/// <summary>
/// Provides administrative endpoints to manage and synchronize the multi-level cache state.
/// </summary>
/// <remarks>
/// This controller allows for manual intervention in the cache lifecycle, supporting both 
/// targeted key removal and broad prefix-based invalidation.
/// </remarks>
[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheController"/> class.
    /// </summary>
    /// <param name="cache">The hybrid cache service instance.</param>
    public CacheController(ICacheService cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Performs a logical invalidation of all keys associated with a specific prefix.
    /// </summary>
    /// <param name="prefix">The grouping prefix (e.g., "catalog" or "identity") to invalidate.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the invalidation request.</returns>
    /// <remarks>
    /// This is a high-performance operation that increments the prefix version in the L2 cache 
    /// and broadcasts a purge message to all application instances to clear their L1 caches.
    /// </remarks>
    [HttpPost("invalidate-prefix/{prefix}")]
    public async Task<IActionResult> InvalidatePrefix(string prefix, CancellationToken ct)
    {
        await _cache.InvalidatePrefixAsync(prefix, ct);
        return Ok($"Prefix '{prefix}' invalidated.");
    }

    /// <summary>
    /// Removes a specific item from both L1 and L2 cache levels.
    /// </summary>
    /// <param name="key">The unique identifier of the cached item to remove.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IActionResult"/> confirming the removal of the specified key.</returns>
    [HttpDelete("{key}")]
    public async Task<IActionResult> Remove(string key, CancellationToken ct)
    {
        await _cache.RemoveAsync(key, ct);
        return Ok($"Key '{key}' removed.");
    }

    /// <summary>
    /// Physically deletes all keys matching the specified prefix from the Redis L2 store.
    /// </summary>
    /// <param name="prefix">The prefix pattern used to identify keys for physical deletion.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IActionResult"/> confirming the physical deletion from Redis.</returns>
    /// <remarks>
    /// <para>
    /// This method triggers a Lua script in Redis to <c>SCAN</c> and <c>DEL</c> matching keys.
    /// </para>
    /// <para>
    /// <b>Caution:</b> This is an expensive operation in Redis. For frequent cache clearing, 
    /// use <see cref="InvalidatePrefix(string, CancellationToken)"/> instead.
    /// </para>
    /// </remarks>
    [HttpPost("remove-by-prefix/{prefix}")]
    public async Task<IActionResult> RemoveByPrefix(string prefix, CancellationToken ct)
    {
        await _cache.RemoveByPrefixAsync(prefix, ct);
        return Ok($"Physical keys with prefix '{prefix}' removed from Redis.");
    }
}
