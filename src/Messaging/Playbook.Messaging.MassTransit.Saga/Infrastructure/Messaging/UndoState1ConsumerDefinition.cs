using MassTransit;

using Playbook.Messaging.MassTransit.Saga.Application.Consumers;

namespace Playbook.Messaging.MassTransit.Saga.Infrastructure.Messaging;

/// <summary>
/// Defines the endpoint configuration and behavioral policies for the <see cref="UndoState1Consumer"/>.
/// Customizes the receive endpoint to include specific resiliency patterns like Circuit Breakers and Exponential Retry.
/// </summary>
public class UndoState1ConsumerDefinition : ConsumerDefinition<UndoState1Consumer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UndoState1ConsumerDefinition"/> class.
    /// Configures a specific endpoint name for the compensation of the first stage.
    /// </summary>
    public UndoState1ConsumerDefinition()
    {
        // Explicitly set the queue name to ensure consistency across environments.
        EndpointName = "saga-undo-state-1";
    }

    /// <summary>
    /// Configures the consumer's receive endpoint, applying middleware for fault tolerance.
    /// </summary>
    /// <param name="endpointConfigurator">The configurator for the receive endpoint.</param>
    /// <param name="consumerConfigurator">The configurator for the specific consumer instance.</param>
    /// <param name="context">The registration context for resolving dependencies.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UndoState1Consumer> consumerConfigurator, IRegistrationContext context)
    {
        // Prevents cascading failures by opening the circuit if the compensation logic fails repeatedly.
        endpointConfigurator.UseCircuitBreaker(cb => { /* config */ });

        // Implements an exponential backoff strategy to handle transient issues during the undo process,
        // starting at 2 seconds and scaling up to 30 seconds.
        endpointConfigurator.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
    }
}
