using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

public sealed class ConsumerRegistry
{
    // Maps Message Type -> List of Handler Types
    private readonly ConcurrentDictionary<Type, List<Type>> _handlerMappings = new();

    public void RegisterHandler<T, THandler>()
        where T : class
        where THandler : IIntegrationEventHandler<T>
    {
        var handlers = _handlerMappings.GetOrAdd(typeof(T), _ => []);
        handlers.Add(typeof(THandler));
    }

    public IEnumerable<Type> GetHandlersForType(Type messageType) =>
        _handlerMappings.TryGetValue(messageType, out var handlers) ? handlers : Enumerable.Empty<Type>();

    public IEnumerable<Type> GetRegisteredMessageTypes() => _handlerMappings.Keys;
}
