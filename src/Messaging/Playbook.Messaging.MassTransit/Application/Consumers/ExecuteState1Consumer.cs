using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

public class ExecuteState1Consumer(ILogger<ExecuteState1Consumer> logger, IChaosService chaos) : IConsumer<ExecuteState1>
{
    public async Task Consume(ConsumeContext<ExecuteState1> context)
    {
        logger.LogInformation("--- [STATE 1] Executing Business Logic ---");
        try
        {
            chaos.ThrowIfUnlucky("State 1");
            // Simulate work
            await Task.Delay(500);
            await context.Publish(new State1Completed(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogError("[STATE 1] Failed");
            await context.Publish(new State1Failed(context.Message.CorrelationId, ex.Message));
        }
    }
}
