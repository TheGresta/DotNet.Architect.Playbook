using System.Collections.Concurrent;

using Microsoft.Extensions.Localization;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Resources;

namespace Playbook.Exceptions.Core;

public sealed class LocalizedStringProvider(
    IStringLocalizerFactory factory,
    ILogger<LocalizedStringProvider> logger) : ILocalizedStringProvider
{
    // Optimization: Cache localizers to avoid factory overhead on every request
    private readonly ConcurrentDictionary<Type, IStringLocalizer> _localizerCache = new();

    public string Get(string key, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        try
        {
            var resourceType = GetResourceType(key);

            // Get or create localizer from cache
            var localizer = _localizerCache.GetOrAdd(resourceType, factory.Create);

            // Optimization: Span-based prefix stripping to avoid string allocations
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

    private static string ExtractKey(string key)
    {
        int index = key.IndexOf('_');
        return index == -1 || index == key.Length - 1
            ? key
            : key[(index + 1)..];
    }

    private static Type GetResourceType(string key)
    {
        // Optimization: Use Span for prefix checking to avoid substring allocations
        ReadOnlySpan<char> span = key.AsSpan();

        if (span.StartsWith(LocalizationPrefixes.Info)) return typeof(InfoResources);
        if (span.StartsWith(LocalizationPrefixes.Detail)) return typeof(DetailResources);
        if (span.StartsWith(LocalizationPrefixes.Resource)) return typeof(ResourceResources);
        if (span.StartsWith(LocalizationPrefixes.Validation)) return typeof(ValidationResources);
        if (span.StartsWith(LocalizationPrefixes.Rule)) return typeof(BusinessRuleResources);

        return typeof(SharedResources);
    }
}
