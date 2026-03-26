using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

public class ExecuteState2Consumer(ILogger<ExecuteState2Consumer> logger, IChaosService chaos) : IConsumer<ExecuteState2>
{
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
            logger.LogError("[STATE 2] Failed");
            await context.Publish(new State2Failed(context.Message.CorrelationId, ex.Message));
        }
    }
}
