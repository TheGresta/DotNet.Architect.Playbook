namespace Playbook.Persistence.HybridCaching.Core.Configuration;

/// <summary>
/// Defines the global configuration settings for the caching infrastructure.
/// </summary>
/// <remarks>
/// These settings control the structural composition of cache keys and the connection 
/// details for the distributed storage provider (e.g., Redis). 
/// Changes to <see cref="SchemaVersion"/> can be used to effectively invalidate 
/// all existing cache entries across the environment by altering the key prefix.
/// </remarks>
public record CacheSettings
{
    /// <summary>
    /// Gets the current schema version of the cached data structures. 
    /// Defaults to "v1". 
    /// </summary>
    /// <remarks>
    /// Incrementing this value ensures that different versions of the application 
    /// do not attempt to deserialize incompatible data from the shared cache.
    /// </remarks>
    public string SchemaVersion { get; init; } = "v1";

    /// <summary>
    /// Gets the application-specific namespace or environment identifier (e.g., "production", "staging").
    /// Defaults to "global".
    /// </summary>
    /// <remarks>
    /// This prevents key collisions when multiple applications or environments 
    /// share the same distributed cache instance.
    /// </remarks>
    public string Namespace { get; init; } = "global";

    /// <summary>
    /// Gets the connection string used to authenticate and connect to the Redis distributed cache.
    /// Defaults to an empty string.
    /// </summary>
    public string RedisConnectionString { get; init; } = string.Empty;
}
