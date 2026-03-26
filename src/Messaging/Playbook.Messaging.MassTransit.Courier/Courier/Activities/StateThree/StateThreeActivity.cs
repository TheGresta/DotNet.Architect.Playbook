using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

namespace Playbook.Messaging.MassTransit.Courier.Courier.Activities.StateThree;

/// <summary>
/// Arguments required for the finalization state of the distributed workflow.
/// </summary>
/// <param name="TransactionId">The unique identifier for the transaction being finalized.</param>
public record StateThreeArgs(Guid TransactionId);

/// <summary>
/// A terminal execution-only activity that finalizes the workflow. 
/// Since it is the final step and assumes idempotency, no compensation log is maintained.
/// </summary>
public class StateThreeActivity(IChaosProvider chaos, ILogger<StateThreeActivity> logger)
    : IExecuteActivity<StateThreeArgs>
{
    /// <summary>
    /// Executes the finalization logic for the entire workflow sequence.
    /// </summary>
    /// <param name="context">The execution context containing <see cref="StateThreeArgs"/>.</param>
    /// <returns>A task representing the asynchronous execution result.</returns>
    public async Task<MassTransit.ExecutionResult> Execute(ExecuteContext<StateThreeArgs> context)
    {
        logger.LogInformation("[FORWARD] State 3: Finalizing transaction...");

        chaos.EnsureStability("State 3 Execution");

        logger.LogInformation("[FINISH] State 3 Completed. Workflow Success!");
        return context.Completed();
    }
}
