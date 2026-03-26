using MassTransit;

using Playbook.Messaging.MassTransit.Saga.Application.Services;
using Playbook.Messaging.MassTransit.Saga.Contracts;

namespace Playbook.Messaging.MassTransit.Saga.Application.Consumers;

/// <summary>
/// Consumer responsible for executing the first stage of the distributed workflow.
/// Handles the core business logic for State 1 and reports success or failure back to the Saga.
/// </summary>
public class ExecuteState1Consumer(ILogger<ExecuteState1Consumer> logger, IChaosService chaos) : IConsumer<ExecuteState1>
{
    /// <summary>
    /// Consumes the <see cref="ExecuteState1"/> command to perform the first step of the workflow.
    /// Includes simulated latency and randomized failure injection for resiliency testing.
    /// </summary>
    /// <param name="context">The consume context containing the command message and correlation data.</param>
    public async Task Consume(ConsumeContext<ExecuteState1> context)
    {
        logger.LogInformation("--- [STATE 1] Executing Business Logic ---");
        try
        {
            // Injects a synthetic failure to test the state machine's error handling and compensation paths.
            chaos.ThrowIfUnlucky("State 1");

            // Simulate asynchronous I/O or processing work.
            await Task.Delay(500);

            await context.Publish(new State1Completed(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[STATE 1] Failed for CorrelationId: {CorrelationId}", context.Message.CorrelationId);
            await context.Publish(new State1Failed(context.Message.CorrelationId, ex.Message));
        }
    }
}
