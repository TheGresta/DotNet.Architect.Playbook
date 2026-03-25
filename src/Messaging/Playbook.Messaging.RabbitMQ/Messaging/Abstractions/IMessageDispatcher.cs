namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Defines the contract for an asynchronous message dispatcher responsible for routing 
/// inbound raw message payloads to their corresponding strongly-typed event handlers.
/// </summary>
/// <remarks>
/// The dispatcher acts as the bridge between the transport layer (e.g., RabbitMQ) and 
/// the application layer, managing serialization concerns and service scope orchestration.
/// </remarks>
internal interface IMessageDispatcher
{
    /// <summary>
    /// Dispatches a raw byte payload to all registered handlers for the specified message type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The message contract type to which the body should be deserialized.</typeparam>
    /// <param name="body">The raw, immutable memory segment containing the serialized message data.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests during the dispatch process.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispatch and execution of all associated handlers.</returns>
    /// <remarks>
    /// Implementation should handle the creation of asynchronous service scopes and manage 
    /// parallel execution of multiple handlers if applicable.
    /// </remarks>
    ValueTask DispatchAsync<T>(ReadOnlyMemory<byte> body, CancellationToken ct) where T : class;
}
