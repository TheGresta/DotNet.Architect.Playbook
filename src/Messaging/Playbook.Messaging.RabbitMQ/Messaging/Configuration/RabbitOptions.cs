namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

/// <summary>
/// Represents the comprehensive configuration suite for the RabbitMQ messaging infrastructure.
/// Contains parameters for connection management, performance tuning, and concurrency control 
/// optimized for .NET 10 high-throughput environments.
/// </summary>
public class RabbitOptions
{
    /// <summary>
    /// Gets or sets the DNS name or IP address of the RabbitMQ broker.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for authenticating with the RabbitMQ broker.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authenticating with the RabbitMQ broker.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the virtual host to use. Defaults to the standard RabbitMQ root <c>"/"</c>.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the port number the RabbitMQ broker is listening on. Defaults to <c>5672</c>.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the maximum number of messages that can be processed in parallel per consumer instance.
    /// This acts as the execution cap for the internal worker pool.
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages the broker will deliver to this consumer.
    /// This provides essential backpressure control to prevent memory exhaustion during message spikes.
    /// </summary>
    public int PrefetchCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum number of <see cref="RabbitMQ.Client.IChannel"/> instances to keep "warm" in the internal pool.
    /// Higher values reduce the latency of channel creation at the cost of increased broker-side resource usage.
    /// </summary>
    public int ChannelPoolSize { get; set; } = 5;
}
