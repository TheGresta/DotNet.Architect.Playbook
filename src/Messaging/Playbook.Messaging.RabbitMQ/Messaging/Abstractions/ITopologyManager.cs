using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Abstractions;

internal interface ITopologyManager
{
    ValueTask EnsureTopologyAsync<T>(IChannel channel, CancellationToken ct) where T : class;
}
