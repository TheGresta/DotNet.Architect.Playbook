using System.Text.Json;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;
using Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Engine.Producer;

internal sealed class RabbitProducer<T>(
    PersistentConnection connection,
    ITopologyManager topologyManager,
    MessageEndpointRegistry registry) : IProducer<T> where T : class
{
    public async ValueTask PublishAsync(T message, CancellationToken ct)
    {
        var definition = registry.GetDefinition<T>();

        await using var lease = await connection.AcquireAsync(ct).ConfigureAwait(false);

        // Ensure infrastructure exists before publishing
        await topologyManager.EnsureTopologyAsync<T>(lease.Channel, ct).ConfigureAwait(false);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, MessagingJsonContext.Default.Options);
        var props = CreateProperties(definition);

        await lease.Channel.BasicPublishAsync(
            exchange: definition.ExchangeName,
            routingKey: definition.RoutingKey ?? string.Empty,
            mandatory: definition.WaitForConfirm,
            basicProperties: props,
            body: body,
            cancellationToken: ct).ConfigureAwait(false);
    }

    public async ValueTask PublishBatchAsync(IEnumerable<T> messages, CancellationToken ct)
    {
        var definition = registry.GetDefinition<T>();

        await using var lease = await connection.AcquireAsync(ct).ConfigureAwait(false);

        await topologyManager.EnsureTopologyAsync<T>(lease.Channel, ct).ConfigureAwait(false);

        foreach (var msg in messages)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(msg, MessagingJsonContext.Default.Options);
            var props = CreateProperties(definition);

            await lease.Channel.BasicPublishAsync(
                exchange: definition.ExchangeName,
                routingKey: definition.RoutingKey ?? string.Empty,
                mandatory: definition.WaitForConfirm,
                basicProperties: props,
                body: body,
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    private static BasicProperties CreateProperties(MessageEndpointDefinition def) => new BasicProperties
    {
        Persistent = true,
        DeliveryMode = DeliveryModes.Persistent, // Ensure messages survive RabbitMQ restart
        Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
    };
}
