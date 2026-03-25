namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

public interface IIntegrationEventHandler<in T> where T : class
{
    Task HandleAsync(T message, CancellationToken ct = default);
}
