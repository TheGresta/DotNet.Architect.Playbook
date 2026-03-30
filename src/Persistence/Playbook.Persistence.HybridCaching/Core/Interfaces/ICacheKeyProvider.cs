namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

public interface ICacheKeyProvider
{
    string GetKey(string prefix, string? identifier = null);
    string[] GetTags(string prefix, string[]? vendorIds);
}
