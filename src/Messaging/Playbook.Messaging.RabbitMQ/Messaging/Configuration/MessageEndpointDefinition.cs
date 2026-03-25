namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

/// <summary>
/// Encapsulates the configuration metadata for a specific messaging endpoint.
/// This record defines the structural and behavioral properties of the RabbitMQ 
/// infrastructure associated with a particular message contract.
/// </summary>
/// <remarks>
/// Used by the <see cref="ITopologyManager"/> to declare exchanges and by 
/// <see cref="RabbitProducer{T}"/> to determine routing and reliability settings.
/// </remarks>
public sealed record MessageEndpointDefinition
{
    /// <summary>
    /// Gets or sets the name of the RabbitMQ exchange. 
    /// Defaults to the message type name if not explicitly configured.
    /// </summary>
    public string ExchangeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing key used for binding and publishing. 
    /// For Fanout exchanges, this is typically an empty string.
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional Time-To-Live (TTL) for messages. 
    /// If set, the broker will automatically expire messages after this duration.
    /// </summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the producer should wait for a 
    /// publisher confirmation from the broker before completing the publish task.
    /// Defaults to <c>true</c> for guaranteed delivery.
    /// </summary>
    public bool WaitForConfirm { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the infrastructure (Exchanges/Queues) 
    /// should be automatically declared by the <see cref="ITopologyManager"/> on first use.
    /// </summary>
    public bool AutoCreate { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the Dead Letter Exchange (DLX). 
    /// Messages that are Nack'd without requeue or expired via TTL will be routed here.
    /// </summary>
    public string? DeadLetterExchange { get; set; }

    /// <summary>
    /// Gets or sets the routing key to be used when a message is moved to the Dead Letter Exchange.
    /// </summary>
    public string? DeadLetterRoutingKey { get; set; }
}
