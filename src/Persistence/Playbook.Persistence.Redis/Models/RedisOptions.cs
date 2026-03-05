namespace Playbook.Persistence.Redis.Models;

public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Full connection string (e.g., "localhost:6379,password=...")
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional prefix for all cache keys (avoids collisions in shared Redis)
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Default expiration for cache entries (if not provided per call)
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; }

    /// <summary>
    /// Whether to abort on connect fail (set to false in production to allow retries)
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// Timeout for synchronous operations
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;
}
