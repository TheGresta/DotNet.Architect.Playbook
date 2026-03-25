namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

public interface IProducer<in T> where T : class
{
    ValueTask PublishAsync(T message, CancellationToken ct = default);
    ValueTask PublishBatchAsync(IEnumerable<T> messages, CancellationToken ct = default);
}
