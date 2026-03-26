using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

/// <summary>
/// Consumer responsible for performing the compensation logic (rollback) for the second stage.
/// Reverses changes introduced during State 2 before the workflow can proceed to undo State 1.
/// </summary>
public class UndoState2Consumer(ILogger<UndoState2Consumer> logger, IChaosService chaos) : IConsumer<UndoState2>
{
    /// <summary>
    /// Consumes the <see cref="UndoState2"/> command.
    /// Successful completion publishes an event that the Saga uses to trigger the next step in the rollback sequence.
    /// </summary>
    /// <param name="context">The consume context for the second stage compensation.</param>
    /// <exception cref="Exception">Re-thrown to allow infrastructure-level error handling.</exception>
    public async Task Consume(ConsumeContext<UndoState2> context)
    {
        logger.LogWarning("<<< [UNDO STATE 2] Reversing database changes for {Id}...", context.Message.CorrelationId);

        try
        {
            chaos.ThrowIfUnlucky("Undo State 2");

            await Task.Delay(500);

            await context.Publish(new State2Undone(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "[UNDO STATE 2] CRITICAL: Rollback failed for {Id}!", context.Message.CorrelationId);

            // Propagation of the exception ensures that the saga does not transition to a 'failed' state
            // until the infrastructure has exhausted its attempts to correct the transient issue.
            throw;
        }
    }
}
