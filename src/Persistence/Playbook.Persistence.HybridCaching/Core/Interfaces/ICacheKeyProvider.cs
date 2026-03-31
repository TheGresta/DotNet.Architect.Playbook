namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

/// <summary>
/// Defines a contract for generating consistent, versioned, and namespaced cache keys and tags.
/// </summary>
public interface ICacheKeyProvider
{
    /// <summary>
    /// Generates a standardized cache key based on a prefix and an optional unique identifier.
    /// </summary>
    /// <param name="prefix">The logical category or domain for the cache entry (e.g., "products").</param>
    /// <param name="identifier">An optional unique ID for the specific entity. Defaults to "default" if null or whitespace.</param>
    /// <returns>A fully qualified cache key string.</returns>
    string GetKey(string prefix, string? identifier = null);

    /// <summary>
    /// Generates a collection of cache tags for bulk invalidation patterns.
    /// </summary>
    /// <param name="prefix">The logical category or domain for the tags.</param>
    /// <param name="vendorIds">An array of unique identifiers to be transformed into tags.</param>
    /// <returns>An array of fully qualified cache tag strings.</returns>
    string[] GetTags(string prefix, string[]? vendorIds);
}
