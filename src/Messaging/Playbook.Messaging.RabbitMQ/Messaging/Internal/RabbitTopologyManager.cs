using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

/// <summary>
/// Manages the idempotent declaration of RabbitMQ infrastructure components (Exchanges, Queues, Bindings).
/// Ensures that messaging topology is established exactly once per message type for the lifetime of the application.
/// </summary>
/// <remarks>
/// This manager uses an internal <see cref="ConcurrentDictionary{TKey, TValue}"/> to track initialization states,
/// providing O(1) lookups for the "hot path" of message publishing or consumption.
/// </remarks>
internal sealed class RabbitTopologyManager(
    MessageEndpointRegistry registry,
    ILogger<RabbitTopologyManager> logger) : ITopologyManager
{
    private readonly ConcurrentDictionary<Type, bool> _initializedTypes = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Verifies and creates the required RabbitMQ exchange topology for a given message type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The message contract type whose topology needs to be ensured.</typeparam>
    /// <param name="channel">The active RabbitMQ <see cref="IChannel"/> used to execute declaration commands.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous initialization operation.</returns>
    public async ValueTask EnsureTopologyAsync<T>(IChannel channel, CancellationToken ct) where T : class
    {
        // High-performance gate check: avoids lock contention for types already initialized.
        if (_initializedTypes.ContainsKey(typeof(T))) return;

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check locking pattern to prevent race conditions during concurrent initialization requests.
            if (_initializedTypes.ContainsKey(typeof(T))) return;

            var definition = registry.GetDefinition<T>();
            if (!definition.AutoCreate)
            {
                _initializedTypes.TryAdd(typeof(T), true);
                return;
            }

            var typeName = typeof(T).Name;
            // Must match the convention in RabbitConsumerEngine<T>: $"{typeof(T).Name}.Queue"
            var queueName = $"{typeName}.Queue";

            // 1. Declare the primary exchange (no TTL — exchanges do not hold messages).
            await DeclareExchangeAsync(channel, definition, ct).ConfigureAwait(false);

            // 2. Optionally declare the Dead Letter Exchange before the main queue references it.
            if (!string.IsNullOrEmpty(definition.DeadLetterExchange))
                await DeclareDlxExchangeAsync(channel, definition.DeadLetterExchange, ct).ConfigureAwait(false);

            // 3. Declare the consumer queue; TTL and DLX args belong here, not on the exchange.
            await DeclareQueueAsync(channel, queueName, definition, ct).ConfigureAwait(false);

            // 4. Bind the queue to the exchange so published messages are routed to it.
            await BindQueueAsync(channel, queueName, definition, ct).ConfigureAwait(false);

            logger.LogInformation(
                "Topology initialized for {Type}: Exchange={Exchange}, Queue={Queue}",
                typeName, definition.ExchangeName, queueName);

            _initializedTypes.TryAdd(typeof(T), true);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Declares the primary exchange as durable Fanout.
    /// Exchanges do not store messages, so TTL must never be set here.
    /// </summary>
    /// <remarks>
    /// Fanout exchanges broadcast every message to all bound queues and ignore routing keys.
    /// If selective routing is needed in the future, change the exchange type and update
    /// <see cref="BindQueueAsync"/> and producer routing-key logic accordingly.
    /// </remarks>
    private static async Task DeclareExchangeAsync(
        IChannel channel,
        MessageEndpointDefinition def,
        CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: def.ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,          // ← TTL removed; it has no effect on an exchange
            cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares the Dead Letter Exchange (DLX) as a durable Direct exchange.
    /// The DLX must exist before any queue that references it via <c>x-dead-letter-exchange</c>
    /// is declared; otherwise, the broker will reject the queue declaration.
    /// </summary>
    private static async Task DeclareDlxExchangeAsync(
        IChannel channel,
        string dlxExchangeName,
        CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: dlxExchangeName,
            type: ExchangeType.Direct,   // Direct allows DLX queues to filter by routing key
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares the consumer queue with optional TTL and dead-letter arguments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>x-message-ttl</c> controls how long a message may remain in <em>this queue</em>
    /// before the broker expires it. It must be a queue argument — the same argument on an
    /// exchange is silently ignored.
    /// </para>
    /// <para>
    /// <c>x-dead-letter-exchange</c> (and the optional <c>x-dead-letter-routing-key</c>)
    /// tells the broker where to route a message when it is Nack'd with <c>requeue: false</c>
    /// or expires via TTL.
    /// </para>
    /// </remarks>
    private static async Task DeclareQueueAsync(
        IChannel channel,
        string queueName,
        MessageEndpointDefinition def,
        CancellationToken ct)
    {
        Dictionary<string, object?>? args = null;

        // TTL: messages older than this in the queue will be expired (and dead-lettered if DLX is set).
        if (def.Ttl.HasValue)
        {
            args ??= [];
            args["x-message-ttl"] = (long)def.Ttl.Value.TotalMilliseconds;
        }

        // DLX: Nack'd (requeue=false) or TTL-expired messages are routed to this exchange.
        if (!string.IsNullOrEmpty(def.DeadLetterExchange))
        {
            args ??= [];
            args["x-dead-letter-exchange"] = def.DeadLetterExchange;

            // Optional: override the routing key used when dead-lettering.
            // Defaults to the message's original routing key when omitted.
            if (!string.IsNullOrEmpty(def.DeadLetterRoutingKey))
                args["x-dead-letter-routing-key"] = def.DeadLetterRoutingKey;
        }

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds the consumer queue to the primary exchange so that messages published
    /// to the exchange are delivered to the queue.
    /// </summary>
    /// <remarks>
    /// For Fanout exchanges the <paramref name="def"/>.RoutingKey is ignored by the broker,
    /// but it is forwarded here for documentation purposes and forward-compatibility
    /// if the exchange type is later changed to Direct or Topic.
    /// </remarks>
    private static async Task BindQueueAsync(
        IChannel channel,
        string queueName,
        MessageEndpointDefinition def,
        CancellationToken ct)
    {
        await channel.QueueBindAsync(
            queue: queueName,
            exchange: def.ExchangeName,
            routingKey: def.RoutingKey,   // ignored for Fanout, meaningful for Direct/Topic
            arguments: null,
            cancellationToken: ct).ConfigureAwait(false);
    }
}
