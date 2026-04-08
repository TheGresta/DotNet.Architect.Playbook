using Microsoft.Extensions.Options;

using Playbook.Persistence.HybridCaching.Core.Configuration;
using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

/// <summary>
/// Provides a concrete implementation of <see cref="ICacheTagFactory"/> that produces
/// fully qualified, schema-versioned, and namespaced cache tag strings from
/// <see cref="CacheTagDescriptor"/> instances.
/// </summary>
/// <remarks>
/// Tag strings follow the convention: <c>tag:{SchemaVersion}:{Namespace}:{prefix}:{dimension}:{value}</c>.
/// This generalizes the previous vendor-only format and allows any number of invalidation dimensions
/// (vendor, region, entity, etc.) to be expressed through <see cref="CacheTagDescriptor"/> without
/// changes to infrastructure code.
/// </remarks>
public sealed class CacheTagFactory(IOptionsMonitor<CacheSettings> settings) : ICacheTagFactory
{
    private CacheSettings Current => settings.CurrentValue;

    /// <inheritdoc/>
    public string[] Build(string prefix, IEnumerable<CacheTagDescriptor>? descriptors)
    {
        if (descriptors is null) return [];

        return [.. descriptors.Select(d =>
            $"tag:{Current.SchemaVersion}:{Current.Namespace}:{prefix}:{d.Dimension}:{d.Value.ToLowerInvariant()}")];
    }
}