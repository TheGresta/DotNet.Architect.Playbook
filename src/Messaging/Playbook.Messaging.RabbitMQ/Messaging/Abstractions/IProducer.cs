namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Defines a high-level, strongly-typed contract for publishing messages to a messaging broker.
/// This interface abstracts the underlying transport logic, allowing the application to 
/// broadcast events or commands without direct dependency on the broker's client library.
/// </summary>
/// <typeparam name="T">The message contract type to be published. Must be a reference type.</typeparam>
public interface IProducer<in T> where T : class
{
    /// <summary>
    /// Publishes a single message asynchronously to the configured exchange or topic.
    /// </summary>
    /// <param name="message">The message instance to be transmitted.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous publication operation.</returns>
    ValueTask PublishAsync(T message, CancellationToken ct = default);

    /// <summary>
    /// Publishes a collection of messages as a batch, optimizing network throughput 
    /// and resource utilization by potentially reusing the same underlying transport channel.
    /// </summary>
    /// <param name="messages">An <see cref="IEnumerable{T}"/> of messages to be published.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous batch publication operation.</returns>
    ValueTask PublishBatchAsync(IEnumerable<T> messages, CancellationToken ct = default);
}
