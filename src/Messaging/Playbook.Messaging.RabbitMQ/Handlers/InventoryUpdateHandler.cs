using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Models;

namespace Playbook.Messaging.RabbitMQ.Handlers;

public sealed class InventoryUpdateHandler(ILogger<InventoryUpdateHandler> logger)
    : IIntegrationEventHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, CancellationToken ct = default)
    {
        // Big Tech Logic: Update stock levels in DB
        logger.LogInformation("[Inventory] Successfully reserved stock for Order: {OrderId}", message.OrderId);

        return Task.CompletedTask;
    }
}
