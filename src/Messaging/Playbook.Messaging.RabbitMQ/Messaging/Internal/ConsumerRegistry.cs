using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

/// <summary>
/// Acts as a central, thread-safe metadata repository for mapping message contract types to their corresponding event handlers.
/// This registry is used by the <see cref="MessageDispatcher"/> to resolve and instantiate handlers at runtime.
/// </summary>
/// <remarks>
/// Internal storage is managed via <see cref="ConcurrentDictionary{TKey, TValue}"/> and <see cref="ConcurrentBag{T}"/> 
/// to support lock-free registration and retrieval during application startup and message processing.
/// </remarks>
public sealed class ConsumerRegistry
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, byte>> _handlerMappings = [];

    /// <summary>
    /// Registers an association between a message type <typeparamref name="T"/> and a specific handler implementation <typeparamref name="THandler"/>.
    /// </summary>
    /// <typeparam name="T">The message contract type (e.g., an integration event).</typeparam>
    /// <typeparam name="THandler">The specific implementation of <see cref="IIntegrationEventHandler{T}"/> that processes the message.</typeparam>
    public void RegisterHandler<T, THandler>()
        where T : class
        where THandler : IIntegrationEventHandler<T>
    {
        // Atomically retrieve or create the collection of handlers for the specific message type
        var handlers = _handlerMappings.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Type, byte>());
        handlers.TryAdd(typeof(THandler), 0); // Idempotent: ignores duplicates
    }

    /// <summary>
    /// Retrieves all registered handler types associated with the specified <paramref name="messageType"/>.
    /// </summary>
    /// <param name="messageType">The <see cref="Type"/> of the message to look up.</param>
    /// <returns>An <see cref="IEnumerable{Type}"/> containing the types of all registered handlers; returns an empty collection if none are found.</returns>
    public IEnumerable<Type> GetHandlersForType(Type messageType) =>
        _handlerMappings.TryGetValue(messageType, out var handlers) ? handlers.Keys : [];
}
