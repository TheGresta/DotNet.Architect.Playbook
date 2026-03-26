using MassTransit;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Application.Consumers;

public class ExecuteState3Consumer(ILogger<ExecuteState3Consumer> logger, IChaosService chaos) : IConsumer<ExecuteState3>
{
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
            logger.LogError("[STATE 3] Failed");
            await context.Publish(new State3Failed(context.Message.CorrelationId, ex.Message));
        }
    }
}
