using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

public class UndoState2Consumer(ILogger<UndoState2Consumer> logger, IChaosService chaos) : IConsumer<UndoState2>
{
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
            logger.LogCritical("[UNDO STATE 2] CRITICAL: Rollback failed for {Id}!", context.Message.CorrelationId);
            throw;
        }
    }
}
