using Microsoft.Extensions.Options;

using Playbook.Persistence.HybridCaching.Core.Configuration;
using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching;

public class CacheKeyProvider(IOptionsMonitor<CacheSettings> settings) : ICacheKeyProvider
{
    private CacheSettings Current => settings.CurrentValue;

    public string GetKey(string prefix, string? identifier)
    {
        // Use "default" if no identifier is provided (e.g. for GetAll methods)
        var id = string.IsNullOrWhiteSpace(identifier)
            ? "default"
            : identifier.ToLowerInvariant();

        return $"{Current.SchemaVersion}:{Current.Namespace}:{prefix}:{id}";
    }

    public string[] GetTags(string prefix, string[]? vendorIds)
    {
        if (vendorIds == null) return [];

        return [.. vendorIds.Select(v => $"tag:{Current.SchemaVersion}:{Current.Namespace}:{prefix}:vendor:{v.ToLowerInvariant()}")];
    }
}
