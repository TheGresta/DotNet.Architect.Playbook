using MassTransit;

using Playbook.Messaging.MassTransit.Application.Consumers;

namespace Playbook.Messaging.MassTransit.Infrastructure.Messaging;

public class UndoState2ConsumerDefinition : ConsumerDefinition<UndoState2Consumer>
{
    public UndoState2ConsumerDefinition()
    {
        // Explicitly set the queue name if needed, or rely on formatter
        EndpointName = "saga-undo-state-2";
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UndoState2Consumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseCircuitBreaker(cb => { /* config */ });
        endpointConfigurator.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
    }
}
