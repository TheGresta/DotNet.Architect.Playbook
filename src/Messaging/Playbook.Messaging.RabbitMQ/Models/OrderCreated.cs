namespace Playbook.Messaging.RabbitMQ.Models;

public record OrderCreated(Guid OrderId, decimal TotalAmount, DateTime CreatedAt);
