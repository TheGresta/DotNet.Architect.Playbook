using Microsoft.Extensions.Options;

using Playbook.Persistence.HybridCaching.Core.Configuration;
using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

/// <summary>
/// Provides a concrete implementation of <see cref="ICacheKeyProvider"/> that utilizes 
/// <see cref="IOptionsMonitor{CacheSettings}"/> to generate environmentally aware keys.
/// </summary>
/// <remarks>
/// This provider enforces a strict naming convention: <c>{SchemaVersion}:{Namespace}:{Prefix}:{Identifier}</c>.
/// By including a <c>SchemaVersion</c>, the provider enables "Blue/Green" cache deployments and 
/// prevents poisoning when data structures change across releases.
/// </remarks>
public class CacheKeyProvider(IOptionsMonitor<CacheSettings> settings) : ICacheKeyProvider
{
    /// <summary>
    /// Gets the current cache configuration settings.
    /// </summary>
    private CacheSettings Current => settings.CurrentValue;

    /// <summary>
    /// Generates a formatted cache key, ensuring identifiers are normalized to lower-case.
    /// </summary>
    /// <param name="prefix">The logical category for the cache entry.</param>
    /// <param name="identifier">The unique ID for the entity.</param>
    /// <returns>A string representing the full path in the cache store.</returns>
    public string GetKey(string prefix, string? identifier)
    {
        // Fallback to a "default" literal to support broad collection caching where a specific ID is absent.
        var id = string.IsNullOrWhiteSpace(identifier)
            ? "default"
            : identifier.ToLowerInvariant();

        return $"{Current.SchemaVersion}:{Current.Namespace}:{prefix}:{id}";
    }

    /// <summary>
    /// Transforms a set of vendor IDs into namespaced cache tags for granular invalidation.
    /// </summary>
    /// <param name="prefix">The logical category for the tags.</param>
    /// <param name="vendorIds">The collection of IDs to tag.</param>
    /// <returns>An array of normalized tag strings. Returns an empty array if <paramref name="vendorIds"/> is null.</returns>
    public string[] GetTags(string prefix, string[]? vendorIds)
    {
        if (vendorIds == null) return [];

        // Uses the C# 12 spread operator and LINQ projection for efficient array construction.
        return [.. vendorIds.Select(v => $"tag:{Current.SchemaVersion}:{Current.Namespace}:{prefix}:vendor:{v.ToLowerInvariant()}")];
    }
}
