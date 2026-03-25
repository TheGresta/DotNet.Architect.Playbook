using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

/// <summary>
/// Acts as a central, thread-safe repository for storing and retrieving messaging endpoint configurations.
/// Maps message contract types to their respective <see cref="MessageEndpointDefinition"/> metadata.
/// </summary>
/// <remarks>
/// This registry is fundamental to the topology management system, providing the necessary 
/// configuration details (e.g., Exchange names, TTLs, Auto-creation flags) required to 
/// establish RabbitMQ infrastructure at runtime.
/// </remarks>
public sealed class MessageEndpointRegistry
{
    private readonly ConcurrentDictionary<Type, MessageEndpointDefinition> _definitions = [];

    /// <summary>
    /// Registers a specific <see cref="MessageEndpointDefinition"/> for a given message contract type <typeparamref name="T"/>.
    /// If a definition already exists for the type, it will be overwritten.
    /// </summary>
    /// <typeparam name="T">The message contract type to configure.</typeparam>
    /// <param name="definition">The configuration metadata defining how the message type behaves within the broker.</param>
    public void AddDefinition<T>(MessageEndpointDefinition definition) => _definitions[typeof(T)] = definition;

    /// <summary>
    /// Retrieves the <see cref="MessageEndpointDefinition"/> associated with the message type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The message contract type whose definition is being requested.</typeparam>
    /// <returns>
    /// The registered <see cref="MessageEndpointDefinition"/> if found; otherwise, returns a default 
    /// definition using the type name as the Exchange name.
    /// </returns>
    /// <remarks>
    /// The fallback to <c>typeof(T).Name</c> ensures that the messaging system can operate with 
    /// sensible convention-based defaults even if explicit configuration is omitted.
    /// </remarks>
    public MessageEndpointDefinition GetDefinition<T>() =>
        _definitions.GetValueOrDefault(typeof(T)) ?? new MessageEndpointDefinition { ExchangeName = typeof(T).Name };
}
