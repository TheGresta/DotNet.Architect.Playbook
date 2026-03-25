using System.Text.Json;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;
using Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Engine.Producer;

/// <summary>
/// Provides a high-performance, strongly-typed implementation for publishing messages to RabbitMQ.
/// Handles connection acquisition, automatic topology management, and optimized serialization 
/// using .NET 10 source-generated contexts.
/// </summary>
/// <typeparam name="T">The message contract type to be published. Must be a reference type.</typeparam>
internal sealed class RabbitProducer<T>(
    PersistentConnection connection,
    ITopologyManager topologyManager,
    MessageEndpointRegistry registry) : IProducer<T> where T : class
{
    /// <summary>
    /// Publishes a single message to the configured RabbitMQ exchange.
    /// Automatically ensures that the necessary exchange topology is created before transmission.
    /// </summary>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous publication operation.</returns>
    public async ValueTask PublishAsync(T message, CancellationToken ct)
    {
        var definition = registry.GetDefinition<T>();

        // Acquire the appropriate channel: confirm-capable (non-pooled) or standard (pooled).
        await using var lease = definition.WaitForConfirm
            ? await connection.AcquireConfirmChannelAsync(ct).ConfigureAwait(false)
            : await connection.AcquireAsync(ct).ConfigureAwait(false);

        // Ensure infrastructure (Exchanges/Bindings) exists before attempting to publish
        // This is idempotent and optimized via an internal cache in the topology manager
        await topologyManager.EnsureTopologyAsync<T>(lease.Channel, ct).ConfigureAwait(false);

        // Utilize Source-Generated JSON options for zero-reflection serialization
        var body = JsonSerializer.SerializeToUtf8Bytes(message, MessagingJsonContext.Default.Options);
        var props = CreateProperties(definition);

        // When PublisherConfirmationTrackingEnabled = true (confirm channel), BasicPublishAsync
        // internally awaits the broker ack before returning — no separate WaitForConfirmsOrDieAsync needed.
        // mandatory = WaitForConfirm: also enforces routing so unroutable messages trigger a BasicReturn.
        await lease.Channel.BasicPublishAsync(
            exchange: definition.ExchangeName,
            routingKey: definition.RoutingKey ?? string.Empty,
            mandatory: definition.WaitForConfirm,
            basicProperties: props,
            body: body,
            cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a batch of messages sequentially using a single acquired channel lease.
    /// Optimizes resource usage by reusing the same <see cref="ChannelLease"/> for the entire collection.
    /// </summary>
    /// <param name="messages">The collection of messages to publish.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous batch publication operation.</returns>
    public async ValueTask PublishBatchAsync(IEnumerable<T> messages, CancellationToken ct)
    {
        var definition = registry.GetDefinition<T>();

        await using var lease = definition.WaitForConfirm
            ? await connection.AcquireConfirmChannelAsync(ct).ConfigureAwait(false)
            : await connection.AcquireAsync(ct).ConfigureAwait(false);

        await topologyManager.EnsureTopologyAsync<T>(lease.Channel, ct).ConfigureAwait(false);

        foreach (var msg in messages)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(msg, MessagingJsonContext.Default.Options);
            var props = CreateProperties(definition);

            // Sequential publication over the leased channel to maintain message order within the batch
            await lease.Channel.BasicPublishAsync(
                exchange: definition.ExchangeName,
                routingKey: definition.RoutingKey ?? string.Empty,
                mandatory: definition.WaitForConfirm,
                basicProperties: props,
                body: body,
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Configures the RabbitMQ <see cref="BasicProperties"/> for the message, 
    /// ensuring persistence and including a UTC timestamp.
    /// </summary>
    /// <param name="def">The endpoint definition containing configuration metadata.</param>
    /// <returns>A configured <see cref="BasicProperties"/> object.</returns>
    private static BasicProperties CreateProperties(MessageEndpointDefinition def) => new()
    {
        Persistent = true,
        DeliveryMode = DeliveryModes.Persistent,
        Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
    };
}
