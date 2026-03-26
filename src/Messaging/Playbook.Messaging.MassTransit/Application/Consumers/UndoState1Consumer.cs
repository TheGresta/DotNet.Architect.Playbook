using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

/// <summary>
/// Consumer responsible for performing the compensation logic (rollback) for the first stage.
/// Reverses any side effects or database changes introduced during the initial execution of State 1.
/// </summary>
public class UndoState1Consumer(ILogger<UndoState1Consumer> logger, IChaosService chaos) : IConsumer<UndoState1>
{
    /// <summary>
    /// Consumes the <see cref="UndoState1"/> command to perform compensation.
    /// Utilizes a "fail-fast and retry" approach by allowing exceptions to propagate to the transport middleware.
    /// </summary>
    /// <param name="context">The consume context containing the compensation request and correlation ID.</param>
    /// <exception cref="Exception">Re-thrown upon failure to trigger MassTransit's retry and circuit breaker policies.</exception>
    public async Task Consume(ConsumeContext<UndoState1> context)
    {
        logger.LogWarning("<<< [UNDO STATE 1] Reversing database changes for {Id}...", context.Message.CorrelationId);

        try
        {
            // Simulate a "Fragile" Rollback scenario (e.g., DB Deadlock during a delete operation).
            // This tests the robustness of the compensation chain.
            chaos.ThrowIfUnlucky("Undo State 1");

            // Simulate the latency of a database or external API reversal.
            await Task.Delay(500);

            await context.Publish(new State1Undone(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "[UNDO STATE 1] CRITICAL: Failed to reverse changes for {Id}!", context.Message.CorrelationId);

            // We THROW here instead of publishing a "Failed" event to the Saga. 
            // This ensures the message remains in the queue for the Retry Policy and eventually moves to the Error queue
            // if the Circuit Breaker trips, preventing data inconsistency.
            throw;
        }
    }
}
