using Playbook.Persistence.HybridCaching.Core.Entities;
using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching.Policies;

/// <summary>
/// Cache policy specifically for lists of <see cref="CasualWayProduct"/>.
/// </summary>
public class CasualWayProductListPolicy : ICachePolicy<List<CasualWayProduct>>
{
    public string Prefix => "casual-way-product";

    // Tags this list for invalidation when vendors 1 or 2 are updated.
    public string[]? VendorIds => ["1", "2"];

    public TimeSpan MemoryCacheExpiration => TimeSpan.FromMinutes(5);

    public TimeSpan DistributedCacheExpiration => TimeSpan.FromMinutes(20);

    public bool BypassMemory => false;
}
