using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

internal sealed class RabbitTopologyManager(
    MessageEndpointRegistry registry,
    ILogger<RabbitTopologyManager> logger) : ITopologyManager
{
    private readonly ConcurrentDictionary<Type, bool> _initializedTypes = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async ValueTask EnsureTopologyAsync<T>(IChannel channel, CancellationToken ct) where T : class
    {
        // High-performance gate check
        if (_initializedTypes.ContainsKey(typeof(T))) return;

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check lock
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

    private static async Task DeclareExchangeAsync(IChannel channel, MessageEndpointDefinition def, CancellationToken ct)
    {
        Dictionary<string, object?>? args = null;

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
