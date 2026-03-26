using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

/// <summary>
/// Consumer responsible for executing the third and final stage of the distributed workflow.
/// Completes the forward processing of the saga instance.
/// </summary>
public class ExecuteState3Consumer(ILogger<ExecuteState3Consumer> logger, IChaosService chaos) : IConsumer<ExecuteState3>
{
    /// <summary>
    /// Consumes the <see cref="ExecuteState3"/> command. 
    /// Successful execution here leads the Saga toward the Finalized state.
    /// </summary>
    /// <param name="context">The consume context for the third workflow stage.</param>
    public async Task Consume(ConsumeContext<ExecuteState3> context)
    {
        logger.LogInformation("--- [STATE 3] Executing Business Logic ---");
        try
        {
            chaos.ThrowIfUnlucky("State 3");

            await Task.Delay(500);

            await context.Publish(new State3Completed(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[STATE 3] Failed for CorrelationId: {CorrelationId}", context.Message.CorrelationId);
            await context.Publish(new State3Failed(context.Message.CorrelationId, ex.Message));
        }
    }
}
