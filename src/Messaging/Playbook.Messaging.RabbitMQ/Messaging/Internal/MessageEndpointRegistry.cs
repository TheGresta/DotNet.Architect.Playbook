using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

public sealed class MessageEndpointRegistry
{
    private readonly ConcurrentDictionary<Type, MessageEndpointDefinition> _definitions = [];

    public void AddDefinition<T>(MessageEndpointDefinition definition) => _definitions[typeof(T)] = definition;

    public MessageEndpointDefinition GetDefinition<T>() =>
        _definitions.GetValueOrDefault(typeof(T)) ?? new MessageEndpointDefinition { ExchangeName = typeof(T).Name };
}
