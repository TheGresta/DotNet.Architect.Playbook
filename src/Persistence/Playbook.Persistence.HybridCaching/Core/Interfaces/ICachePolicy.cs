namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

/// <summary>
/// Defines the configuration and invalidation rules for a specific cached type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// Implementing this interface allows the <see cref="ICacheProvider"/> to determine 
/// unique naming prefixes, tag-based invalidation boundaries, and multi-layered expiration timeouts.
/// </remarks>
/// <typeparam name="T">The type of data to which this policy applies.</typeparam>
public interface ICachePolicy<T> where T : class
{
    /// <summary>
    /// Gets the logical prefix used to categorize this data type within the cache store.
    /// </summary>
    string Prefix { get; }

    /// <summary>
    /// Gets the collection of vendor or entity identifiers used to generate cache tags for granular invalidation.
    /// </summary>
    string[]? VendorIds { get; }

    /// <summary>
    /// Gets the duration for which the data should remain in the local (L1) in-memory cache.
    /// </summary>
    TimeSpan MemoryCacheExpiration { get; }

    /// <summary>
    /// Gets the duration for which the data should remain in the distributed (L2) cache (e.g., Redis).
    /// </summary>
    TimeSpan DistributedCacheExpiration { get; }

    /// <summary>
    /// Gets a value indicating whether to bypass the local in-memory cache and fetch directly from the distributed layer.
    /// </summary>
    bool BypassMemory { get; }
}
