namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

/// <summary>
/// Provides a fluent builder interface to configure the messaging endpoint metadata for a specific message type <typeparamref name="T"/>.
/// This configuration dictates how exchanges are declared, how messages are routed, and how reliability features 
/// like TTL and Dead Lettering are applied.
/// </summary>
/// <typeparam name="T">The message contract type to be configured.</typeparam>
public sealed class MessageTypeBuilder<T> where T : class
{
    private readonly MessageEndpointDefinition _definition = new()
    {
        // Default convention: Use the class name as the exchange name if not overridden.
        ExchangeName = typeof(T).Name
    };

    /// <summary>
    /// Explicitly sets the name of the RabbitMQ exchange for this message type.
    /// </summary>
    /// <param name="name">The target exchange name.</param>
    /// <returns>The current <see cref="MessageTypeBuilder{T}"/> instance for method chaining.</returns>
    public MessageTypeBuilder<T> ToExchange(string name)
    {
        _definition.ExchangeName = name;
        return this;
    }

    /// <summary>
    /// Configures a specific routing key to be used when publishing messages of this type.
    /// </summary>
    /// <param name="key">The routing key string.</param>
    /// <returns>The current <see cref="MessageTypeBuilder{T}"/> instance for method chaining.</returns>
    public MessageTypeBuilder<T> WithRoutingKey(string key)
    {
        _definition.RoutingKey = key;
        return this;
    }

    /// <summary>
    /// Sets a Time-To-Live (TTL) for messages. Messages older than this duration will be 
    /// expired by the broker (and potentially moved to a Dead Letter Exchange).
    /// </summary>
    /// <param name="ttl">The duration a message should remain valid.</param>
    /// <returns>The current <see cref="MessageTypeBuilder{T}"/> instance for method chaining.</returns>
    public MessageTypeBuilder<T> WithTtl(TimeSpan ttl)
    {
        _definition.Ttl = ttl;
        return this;
    }

    /// <summary>
    /// Disables the wait for publisher confirmations. This increases throughput but 
    /// reduces reliability as the producer will not verify if the broker successfully received the message.
    /// </summary>
    /// <returns>The current <see cref="MessageTypeBuilder{T}"/> instance for method chaining.</returns>
    public MessageTypeBuilder<T> SetFireAndForget()
    {
        _definition.WaitForConfirm = false;
        return this;
    }

    /// <summary>
    /// Configures Dead Letter Exchange (DLX) settings for the message type. 
    /// Failed or expired messages will be routed to the specified exchange.
    /// </summary>
    /// <param name="exchangeName">The name of the exchange to receive dead-lettered messages.</param>
    /// <param name="routingKey">Optional routing key for the DLX. Defaults to [TypeName].Error if null.</param>
    /// <returns>The current <see cref="MessageTypeBuilder{T}"/> instance for method chaining.</returns>
    public MessageTypeBuilder<T> WithDeadLetter(string exchangeName, string? routingKey = null)
    {
        _definition.DeadLetterExchange = exchangeName;
        // Defaulting the routing key to a standard error convention to ensure traceability
        _definition.DeadLetterRoutingKey = routingKey ?? $"{typeof(T).Name}.Error";
        return this;
    }

    /// <summary>
    /// Internal method to finalize the configuration and return the resulting <see cref="MessageEndpointDefinition"/>.
    /// </summary>
    /// <returns>A populated <see cref="MessageEndpointDefinition"/> instance.</returns>
    internal MessageEndpointDefinition Build() => _definition;
}
