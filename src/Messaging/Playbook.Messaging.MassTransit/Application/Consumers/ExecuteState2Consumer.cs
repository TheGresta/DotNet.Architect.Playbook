using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

/// <summary>
/// Consumer responsible for executing the second stage of the distributed workflow.
/// Coordinates the specific logic required for State 2 within the saga sequence.
/// </summary>
public class ExecuteState2Consumer(ILogger<ExecuteState2Consumer> logger, IChaosService chaos) : IConsumer<ExecuteState2>
{
    /// <summary>
    /// Consumes the <see cref="ExecuteState2"/> command.
    /// If successful, triggers the transition to the next state; otherwise, initiates a failure sequence.
    /// </summary>
    /// <param name="context">The consume context for the second workflow stage.</param>
    public async Task Consume(ConsumeContext<ExecuteState2> context)
    {
        logger.LogInformation("--- [STATE 2] Executing Business Logic ---");
        try
        {
            chaos.ThrowIfUnlucky("State 2");

            await Task.Delay(500);

            await context.Publish(new State2Completed(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[STATE 2] Failed for CorrelationId: {CorrelationId}", context.Message.CorrelationId);
            await context.Publish(new State2Failed(context.Message.CorrelationId, ex.Message));
        }
    }
}
