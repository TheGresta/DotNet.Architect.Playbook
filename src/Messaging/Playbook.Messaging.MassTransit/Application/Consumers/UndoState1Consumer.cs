using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

public class UndoState1Consumer(ILogger<UndoState1Consumer> logger, IChaosService chaos) : IConsumer<UndoState1>
{
    public async Task Consume(ConsumeContext<UndoState1> context)
    {
        logger.LogWarning("<<< [UNDO STATE 1] Reversing database changes for {Id}...", context.Message.CorrelationId);

        try
        {
            // Simulate a "Fragile" Rollback (e.g., DB Deadlock during delete)
            chaos.ThrowIfUnlucky("Undo State 1");

            await Task.Delay(500);
            await context.Publish(new State1Undone(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogCritical("[UNDO STATE 1] CRITICAL: Failed to reverse changes for {Id}!", context.Message.CorrelationId);

            // We THROW here instead of publishing a "Failed" event. 
            // This allows MassTransit's Retry and Circuit Breaker to take control.
            throw;
        }
    }
}
