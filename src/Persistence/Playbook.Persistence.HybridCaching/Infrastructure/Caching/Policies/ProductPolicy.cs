using Playbook.Persistence.HybridCaching.Core.Entities;
using Playbook.Persistence.HybridCaching.Core.Interfaces;

namespace Playbook.Persistence.HybridCaching.Infrastructure.Caching.Policies;

public class ProductPolicy : ICachePolicy<Product>
{
    public string Prefix => "product";

    public string[]? VendorIds => ["1"];

    public TimeSpan MemoryCacheExpiration => TimeSpan.FromMinutes(5);

    public TimeSpan DistributedCacheExpiration => TimeSpan.FromMinutes(20);

    public bool BypassMemory => false;
}
