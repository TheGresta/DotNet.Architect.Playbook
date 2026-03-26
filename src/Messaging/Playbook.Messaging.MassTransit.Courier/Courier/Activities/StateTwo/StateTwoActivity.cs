using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

namespace Playbook.Messaging.MassTransit.Courier.Courier.Activities.StateTwo;

/// <summary>
/// Arguments required to process the second state of the distributed workflow.
/// </summary>
/// <param name="TransactionId">The unique identifier for the transaction.</param>
public record StateTwoArgs(Guid TransactionId);
/// <summary>
/// Persistence log used to track process identifiers for potential compensation in State Two.
/// </summary>
/// <param name="ProcessId">The external or internal process identifier generated during execution.</param>
public record StateTwoLog(string ProcessId);

/// <summary>
/// Implements the intermediate processing state of the routing slip with full compensation support.
/// </summary>
public class StateTwoActivity(IChaosProvider chaos, ILogger<StateTwoActivity> logger)
    : IActivity<StateTwoArgs, StateTwoLog>
{
    /// <summary>
    /// Executes the data processing logic for State Two.
    /// </summary>
    /// <param name="context">The execution context containing <see cref="StateTwoArgs"/>.</param>
    /// <returns>A task representing the asynchronous execution result.</returns>
    public async Task<MassTransit.ExecutionResult> Execute(ExecuteContext<StateTwoArgs> context)
    {
        logger.LogInformation("[FORWARD] State 2: Processing data...");

        chaos.EnsureStability("State 2 Execution");

        logger.LogInformation("[SUCCESS] State 2 Completed.");

        // Passing a specific ProcessId to the log to ensure compensation targets the correct resource
        return context.Completed(new StateTwoLog("PROC-99"));
    }

    /// <summary>
    /// Reverts the specific process initiated during the execution phase.
    /// </summary>
    /// <param name="context">The compensation context containing the <see cref="StateTwoLog"/>.</param>
    /// <returns>A task representing the asynchronous compensation result.</returns>
    public async Task<CompensationResult> Compensate(CompensateContext<StateTwoLog> context)
    {
        logger.LogInformation("[BACKWARD] State 2: Reverting Process {Id}", context.Log.ProcessId);
        return context.Compensated();
    }
}
