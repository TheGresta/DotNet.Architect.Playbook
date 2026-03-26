using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

namespace Playbook.Messaging.MassTransit.Courier.Courier.Activities.StateThree;

public record StateThreeArgs(Guid TransactionId);

// No log needed if there is nothing to undo in the final step 
// (or if the final step is non-transactional)
public class StateThreeActivity(IChaosProvider chaos, ILogger<StateThreeActivity> logger)
    : IExecuteActivity<StateThreeArgs>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<StateThreeArgs> context)
    {
        logger.LogInformation("[FORWARD] State 3: Finalizing transaction...");

        chaos.EnsureStability("State 3 Execution");

        logger.LogInformation("[FINISH] State 3 Completed. Workflow Success!");
        return context.Completed();
    }
}
