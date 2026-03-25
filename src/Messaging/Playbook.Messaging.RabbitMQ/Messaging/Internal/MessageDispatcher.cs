using System.Text.Json;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

/// <summary>
/// Orchestrates the deserialization and delivery of incoming messages to their respective handlers.
/// Implements a scoped execution pattern to ensure that each message is processed within a 
/// dedicated <see cref="IServiceScope"/>, preventing resource leakage across concurrent operations.
/// </summary>
/// <remarks>
/// This dispatcher leverages .NET 10 System.Text.Json source generation for high-performance 
/// deserialization and executes multiple handlers for the same message type in parallel.
/// </remarks>
internal sealed class MessageDispatcher(
    IServiceScopeFactory scopeFactory,
    ConsumerRegistry consumerRegistry,
    ILogger<MessageDispatcher> logger) : IMessageDispatcher
{
    /// <summary>
    /// Processes an incoming raw message body by deserializing it into <typeparamref name="T"/> 
    /// and invoking all registered integration event handlers.
    /// </summary>
    /// <typeparam name="T">The contract type of the message being dispatched.</typeparam>
    /// <param name="body">The raw message payload as a <see cref="ReadOnlyMemory{Byte}"/>.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispatch operation.</returns>
    /// <exception cref="Exception">
    /// Thrown if one or more handlers fail, allowing the calling consumer engine 
    /// to handle negative acknowledgments or dead-lettering.
    /// </exception>
    public async ValueTask DispatchAsync<T>(ReadOnlyMemory<byte> body, CancellationToken ct) where T : class
    {
        // 1. Create a scoped provider for this specific message to isolate dependency lifetimes (e.g., DbContext)
        await using var scope = scopeFactory.CreateAsyncScope();

        // 2. Deserialize using the Source-Generated context for optimized performance and reduced reflection overhead
        var message = JsonSerializer.Deserialize<T>(body.Span, MessagingJsonContext.Default.Options);
        if (message is null)
        {
            logger.LogError("Message of type {Type} was null after deserialization.", typeof(T).Name);
            return;
        }

        // 3. Resolve all handler types registered for this specific message contract
        var handlerTypes = consumerRegistry.GetHandlersForType(typeof(T));
        if (!handlerTypes.Any())
        {
            logger.LogWarning("No handlers found for {Type}. Message skipped.", typeof(T).Name);
            return;
        }

        // 4. Instantiate and execute all handlers in parallel to minimize total processing latency
        var tasks = handlerTypes.Select(handlerType =>
        {
            var handler = (IIntegrationEventHandler<T>)scope.ServiceProvider.GetRequiredService(handlerType);
            return handler.HandleAsync(message, ct);
        }).ToList(); // Materialize to catch resolution failures immediately

        try
        {
            // Await completion of all handlers; failures are aggregated and caught below
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "One or more handlers failed for message {Type}", typeof(T).Name);

            throw new InvalidOperationException($"Failed to deserialize message of type {typeof(T).Name}. Message will be routed to DLX.");
        }
    }
}
