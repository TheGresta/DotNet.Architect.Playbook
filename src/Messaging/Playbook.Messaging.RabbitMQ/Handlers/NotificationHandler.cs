using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Models;

namespace Playbook.Messaging.RabbitMQ.Handlers;

public sealed class NotificationHandler(ILogger<NotificationHandler> logger)
    : IIntegrationEventHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, CancellationToken ct = default)
    {
        // Big Tech Logic: Send Push/Email to customer
        logger.LogInformation("[Notification] Dispatching confirmation email for Order: {OrderId}", message.OrderId);

        return Task.CompletedTask;
    }
}
