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

            await DeclareExchangeAsync(channel, definition, ct).ConfigureAwait(false);

            logger.LogInformation("Topology initialized for {Type} on Exchange: {Exchange}",
                typeof(T).Name, definition.ExchangeName);

            _initializedTypes.TryAdd(typeof(T), true);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Executes the physical exchange declaration against the broker.
    /// Configures standard RabbitMQ properties such as durability, auto-deletion, and TTL arguments.
    /// </summary>
    /// <param name="channel">The channel to perform the declaration on.</param>
    /// <param name="def">The endpoint definition containing exchange configurations.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task DeclareExchangeAsync(IChannel channel, MessageEndpointDefinition def, CancellationToken ct)
    {
        Dictionary<string, object?>? args = null;

        // Map high-level TTL configuration to RabbitMQ's specific x-message-ttl argument.
        if (def.Ttl.HasValue)
        {
            args = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (long)def.Ttl.Value.TotalMilliseconds
            };
        }

        // Standardizing on Fanout as per previous Pub/Sub architecture 
        // since ExchangeType was removed from the definition.
        await channel.ExchangeDeclareAsync(
            exchange: def.ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: args,
            cancellationToken: ct).ConfigureAwait(false);
    }
}
