namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

public interface ICachePolicy<T> where T : class
{
    string Prefix { get; }
    string[]? VendorIds { get; }
    TimeSpan MemoryCacheExpiration { get; }
    TimeSpan DistributedCacheExpiration { get; }
    bool BypassMemory { get; }
}
