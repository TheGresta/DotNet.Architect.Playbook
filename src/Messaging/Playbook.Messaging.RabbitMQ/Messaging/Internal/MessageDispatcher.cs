using System.Text.Json;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

internal sealed class MessageDispatcher(
    IServiceScopeFactory scopeFactory,
    ConsumerRegistry consumerRegistry,
    ILogger<MessageDispatcher> logger) : IMessageDispatcher
{
    public async ValueTask DispatchAsync<T>(ReadOnlyMemory<byte> body, CancellationToken ct) where T : class
    {
        // 1. Create a scoped provider for this specific message
        await using var scope = scopeFactory.CreateAsyncScope();

        // 2. Deserialize using the Source-Generated context for performance
        var message = JsonSerializer.Deserialize<T>(body.Span, MessagingJsonContext.Default.Options);
        if (message is null)
        {
            logger.LogError("Message of type {Type} was null after deserialization.", typeof(T).Name);
            return;
        }

        // 3. Resolve all handlers registered for this type
        var handlerTypes = consumerRegistry.GetHandlersForType(typeof(T));
        if (!handlerTypes.Any())
        {
            logger.LogWarning("No handlers found for {Type}. Message skipped.", typeof(T).Name);
            return;
        }

        // 4. Execute all handlers in parallel
        var tasks = handlerTypes.Select(handlerType =>
        {
            var handler = (IIntegrationEventHandler<T>)scope.ServiceProvider.GetRequiredService(handlerType);
            return handler.HandleAsync(message, ct);
        });

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "One or more handlers failed for message {Type}", typeof(T).Name);
            throw; // Re-throw so the Consumer Engine can Nack/DLX the message
        }
    }
}
