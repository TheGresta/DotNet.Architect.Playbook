namespace Playbook.Persistence.HybridCaching.Core.Configuration;

public record CacheSettings
{
    public string SchemaVersion { get; init; } = "v1";
    public string Namespace { get; init; } = "global";
    public string RedisConnectionString { get; init; } = string.Empty;
}
