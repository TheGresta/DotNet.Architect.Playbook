using Microsoft.Extensions.Caching.Memory;

using Playbook.Exceptions.Abstraction;

namespace Playbook.Exceptions.Core;

/// <summary>
/// Provides an implementation of <see cref="ILocalizedStringProvider"/> that retrieves localized 
/// templates from a remote source (e.g., Database, External API, or Distributed Cache).
/// This provider implements a Cache-Aside pattern using <see cref="IMemoryCache"/> to 
/// mitigate latency associated with remote lookups.
/// </summary>
public class RemoteLocalizedStringProvider(
    IMemoryCache cache) : ILocalizedStringProvider
{
    /// <summary>
    /// Retrieves a localized string by checking the local memory cache before attempting 
    /// a remote fetch. The resulting template is then formatted with the provided arguments.
    /// </summary>
    /// <param name="key">The unique identifier for the localized resource.</param>
    /// <param name="args">Arguments used to populate placeholders within the localized template.</param>
    /// <returns>
    /// The formatted localized string. If the key is not found, the original key 
    /// is returned as a fallback to prevent UI breakage.
    /// </returns>
    public string Get(string key, params object[] args)
    {
        // 1. Check local cache first for performance
        // Cache-aside pattern: Reduces high-latency calls to external systems for frequently accessed keys.
        if (!cache.TryGetValue(key, out string? template))
        {
            // 2. If not in cache, get from your source
            template = string.Empty; // Replace with actual retrieval logic, e.g., from a database or remote service    

            // Sliding/Absolute expiration should be tuned based on the frequency of remote content updates.
            cache.Set(key, template, TimeSpan.FromHours(1));
        }

        // Uses string.Format as the interpolation engine. 
        // Note: If 'template' is null, it falls back to the 'key' itself to ensure visibility.
        return string.Format(template ?? key, args);
    }
}
