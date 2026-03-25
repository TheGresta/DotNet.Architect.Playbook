using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

public sealed class MessageEndpointRegistry
{
    private readonly ConcurrentDictionary<Type, MessageEndpointDefinition> _definitions = [];

    public void AddDefinition<T>(MessageEndpointDefinition definition) => _definitions[typeof(T)] = definition;

    public MessageEndpointDefinition GetDefinition<T>()
    {
        return _definitions.TryGetValue(typeof(T), out var definition)
               ? definition
               : new MessageEndpointDefinition { ExchangeName = typeof(T).Name };
    }
}
