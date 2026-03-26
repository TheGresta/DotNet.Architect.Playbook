using MassTransit;

using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

public class CriticalFailureConsumer(ILogger<CriticalFailureConsumer> logger)
    : IConsumer<Fault<UndoState1>>,
      IConsumer<Fault<UndoState2>>
{
    public async Task Consume(ConsumeContext<Fault<UndoState1>> context)
    {
        var originalMessage = context.Message.Message;
        var exceptions = context.Message.Exceptions;

        // In a real company, this triggers PagerDuty / Slack Alert
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

    public async Task Consume(ConsumeContext<Fault<UndoState2>> context)
    {
        logger.LogCritical("[FATAL ERROR] UndoState2 failed for {Id}. Alerting DevOps...",
            context.Message.Message.CorrelationId);

        await Task.CompletedTask;
    }
}
