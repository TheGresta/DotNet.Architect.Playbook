using MassTransit;

using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

/// <summary>
/// A specialized "Dead Letter" or Fault consumer designed to handle terminal failures in the compensation logic.
/// This consumer intercepts <see cref="Fault{T}"/> messages when the transport-level retries for undo operations are exhausted.
/// It serves as the last line of defense for maintaining data integrity through manual intervention alerts.
/// </summary>
/// <param name="logger">The <see cref="ILogger"/> used to record critical system failures and alert operators.</param>
public class CriticalFailureConsumer(ILogger<CriticalFailureConsumer> logger)
    : IConsumer<Fault<UndoState1>>,
      IConsumer<Fault<UndoState2>>
{
    /// <summary>
    /// Consumes a fault specifically for the <see cref="UndoState1"/> command.
    /// This indicates that the system was unable to automatically revert changes for the first stage.
    /// </summary>
    /// <param name="context">The fault context containing the original message and the collection of exceptions that occurred.</param>
    public async Task Consume(ConsumeContext<Fault<UndoState1>> context)
    {
        // Extract the original command that failed and the exception details for diagnostic reporting.
        var originalMessage = context.Message.Message;
        var exceptions = context.Message.Exceptions;

        // In a production enterprise environment, this block would integrate with external alerting 
        // providers (e.g., PagerDuty, Opsgenie, or Slack) to notify an On-Call engineer.
        logger.LogCritical(@"
            [FATAL ERROR] Automated Rollback Failed!
            Step: UndoState1
            CorrelationId: {CorrelationId}
            Error: {ErrorMessage}
            Action: Manual database cleanup required immediately.",
            originalMessage.CorrelationId,
            exceptions.FirstOrDefault()?.Message);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Consumes a fault specifically for the <see cref="UndoState2"/> command.
    /// Triggered when the second stage of compensation fails repeatedly and requires DevOps attention.
    /// </summary>
    /// <param name="context">The fault context for the failed second stage undo operation.</param>
    public async Task Consume(ConsumeContext<Fault<UndoState2>> context)
    {
        // Log a critical alert focusing on the specific correlation ID to facilitate rapid tracing in logs.
        logger.LogCritical("[FATAL ERROR] UndoState2 failed for {Id}. Alerting DevOps...",
            context.Message.Message.CorrelationId);

        await Task.CompletedTask;
    }
}
