using System.ComponentModel.DataAnnotations;

namespace Playbook.Persistence.Redis.Models;

/// <summary>
/// Represents the configuration settings for the Redis L2 cache provider.
/// </summary>
/// <remarks>
/// These options are typically bound from the <c>"Redis"</c> section of the application configuration 
/// (e.g., <c>appsettings.json</c>).
/// </remarks>
public class RedisOptions
{
    /// <summary>
    /// The default section name in the configuration provider.
    /// </summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    /// <value>
    /// A string containing the host, port, and security credentials required to connect to the Redis instance.
    /// </value>
    /// <remarks>
    /// This property is required and cannot be an empty string.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a logical name for the Redis instance, used as a prefix for all keys.
    /// </summary>
    /// <value>
    /// The prefix applied to every key stored in Redis to avoid collisions between different environments or applications.
    /// </value>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default duration a value remains in the cache if no specific expiration is provided.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the default TTL (Time-To-Live). If <see langword="null"/>, 
    /// items may persist indefinitely depending on the Redis server configuration.
    /// </value>
    public TimeSpan? DefaultExpiration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the connection attempt should be aborted if the server is unreachable.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the application should fail fast; <see langword="false"/> to allow 
    /// the client to attempt background reconnection. Defaults to <see langword="false"/>.
    /// </value>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// Gets or sets the time, in milliseconds, allowed for synchronous operations.
    /// </summary>
    /// <value>
    /// The timeout duration for synchronous Redis commands. Defaults to 5000ms (5 seconds).
    /// </value>
    /// <remarks>
    /// This value is constrained between 100ms and 30,000ms. Setting this too low can result in 
    /// <c>TimeoutExceptions</c> during periods of high network latency or CPU thread starvation.
    /// </remarks>
    [Range(100, 30000)]
    public int SyncTimeout { get; set; } = 5000;
}
