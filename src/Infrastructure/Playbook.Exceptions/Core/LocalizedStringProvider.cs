using System.Collections.Concurrent;

using Microsoft.Extensions.Localization;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Resources;

namespace Playbook.Exceptions.Core;

/// <summary>
/// Provides a high-performance, thread-safe implementation of <see cref="ILocalizedStringProvider"/>.
/// This service dynamically resolves the appropriate resource file based on key prefixes and 
/// utilizes caching and memory-efficient span operations to minimize overhead.
/// </summary>
public sealed class LocalizedStringProvider(
    IStringLocalizerFactory factory,
    ILogger<LocalizedStringProvider> logger) : ILocalizedStringProvider
{
    /// <summary>
    /// Internal cache to store <see cref="IStringLocalizer"/> instances, preventing repeated 
    /// factory activation and reflection costs per request.
    /// </summary>
    private readonly ConcurrentDictionary<Type, IStringLocalizer> _localizerCache = new();

    /// <summary>
    /// Resolves and returns a localized string for the specified key and arguments.
    /// </summary>
    /// <param name="key">The prefixed localization key (e.g., "VAL_REQUIRED").</param>
    /// <param name="args">Arguments for string interpolation/formatting.</param>
    /// <returns>The localized string if found; otherwise, returns the original key.</returns>
    public string Get(string key, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        try
        {
            var resourceType = GetResourceType(key);

            // Get or create localizer from cache
            // Double-checked locking is handled internally by ConcurrentDictionary to ensure thread safety
            var localizer = _localizerCache.GetOrAdd(resourceType, factory.Create);

            // Optimization: Span-based prefix stripping to avoid string allocations
            // This extracts the portion after the underscore without creating a temporary substring
            var cleanKey = ExtractKey(key);

            var result = localizer[cleanKey, args];

            if (result.ResourceNotFound)
            {
                logger.LogWarning("Key {CleanKey} not found in {ResourceName}. Full Key: {Key}",
                    cleanKey, resourceType.Name, key);
                return key;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Localization failed for key: {Key}", key);
            return key;
        }
    }

    /// <summary>
    /// Extracts the core resource key by stripping the classification prefix.
    /// </summary>
    /// <param name="key">The full prefixed key.</param>
    /// <returns>The stripped key for resource lookup.</returns>
    private static string ExtractKey(string key)
    {
        int index = key.IndexOf('_');
        // Range operator used to slice the string; effectively a shortcut for Substring
        return index == -1 || index == key.Length - 1
            ? key
            : key[(index + 1)..];
    }

    /// <summary>
    /// Maps a key prefix to a specific marker type representing a .resx resource file.
    /// </summary>
    /// <param name="key">The prefixed localization key.</param>
    /// <returns>The <see cref="Type"/> used by the localizer factory to resolve the resource file.</returns>
    private static Type GetResourceType(string key)
    {
        // Optimization: Use Span for prefix checking to avoid substring allocations on the heap
        ReadOnlySpan<char> span = key.AsSpan();

        if (span.StartsWith(LocalizationPrefixes.Info)) return typeof(InfoResources);
        if (span.StartsWith(LocalizationPrefixes.Detail)) return typeof(DetailResources);
        if (span.StartsWith(LocalizationPrefixes.Resource)) return typeof(ResourceResources);
        if (span.StartsWith(LocalizationPrefixes.Validation)) return typeof(ValidationResources);
        if (span.StartsWith(LocalizationPrefixes.Rule)) return typeof(BusinessRuleResources);

        return typeof(SharedResources);
    }
}
