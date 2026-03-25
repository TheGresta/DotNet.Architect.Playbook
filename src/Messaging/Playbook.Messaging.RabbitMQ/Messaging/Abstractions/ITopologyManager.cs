using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Defines the contract for managing the infrastructure requirements of the messaging system.
/// The topology manager is responsible for the idempotent declaration of exchanges, queues, 
/// and bindings to ensure the broker is correctly configured before message interaction occurs.
/// </summary>
internal interface ITopologyManager
{
    /// <summary>
    /// Asynchronously ensures that the required RabbitMQ topology for the specified message type <typeparamref name="T"/> 
    /// is established on the provided <paramref name="channel"/>.
    /// </summary>
    /// <typeparam name="T">The message contract type that dictates the topology requirements.</typeparam>
    /// <param name="channel">The active <see cref="IChannel"/> to be used for issuing declaration commands to the broker.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Implementations should ensure this operation is idempotent and optimized to minimize 
    /// redundant network calls to the RabbitMQ broker.
    /// </remarks>
    ValueTask EnsureTopologyAsync<T>(IChannel channel, CancellationToken ct) where T : class;
}
