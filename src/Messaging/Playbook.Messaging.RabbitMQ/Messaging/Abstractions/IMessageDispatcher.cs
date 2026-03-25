namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

internal interface IMessageDispatcher
{
    ValueTask DispatchAsync<T>(ReadOnlyMemory<byte> body, CancellationToken ct) where T : class;
}
