namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Defines a strongly-typed contract for handling integration events or messages 
/// within the asynchronous messaging ecosystem. 
/// </summary>
/// <typeparam name="T">The message contract type to be processed. Must be a reference type.</typeparam>
/// <remarks>
/// Implementations of this interface are typically registered as scoped services 
/// and invoked by the <see cref="IMessageDispatcher"/> upon the receipt of a message 
/// of type <typeparamref name="T"/> from the broker.
/// </remarks>
public interface IIntegrationEventHandler<in T> where T : class
{
    /// <summary>
    /// Asynchronously processes the incoming message of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="message">The deserialized message instance to be handled.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> used to monitor for operation cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous handling operation.</returns>
    /// <remarks>
    /// Exceptions thrown from this method will typically trigger a negative acknowledgment (Nack) 
    /// at the transport level, potentially moving the message to a Dead Letter Exchange 
    /// depending on the configured topology.
    /// </remarks>
    Task HandleAsync(T message, CancellationToken ct = default);
}
