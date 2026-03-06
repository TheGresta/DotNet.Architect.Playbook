using Microsoft.Extensions.Caching.Memory;
using Playbook.Exceptions.Abstraction;

namespace Playbook.Exceptions.Core;

public class RemoteLocalizedStringProvider(
    IMemoryCache cache) : ILocalizedStringProvider
{
    public string Get(string key, params object[] args)
    {
        // 1. Check local cache first for performance
        if (!cache.TryGetValue(key, out string? template))
        {
            // 2. If not in cache, get from your source
            template = string.Empty;
            cache.Set(key, template, TimeSpan.FromHours(1));
        }

        return string.Format(template ?? key, args);
    }
}
