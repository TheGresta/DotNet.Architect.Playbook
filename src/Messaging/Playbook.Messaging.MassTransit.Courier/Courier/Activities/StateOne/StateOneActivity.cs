using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

namespace Playbook.Messaging.MassTransit.Courier.Courier.Activities.StateOne;

/// <summary>
/// Arguments required to execute the first state of the distributed workflow.
/// </summary>
/// <param name="TransactionId">The unique identifier for the transaction.</param>
/// <param name="Data">The specific data payload to be processed in this state.</param>
public record StateOneArgs(Guid TransactionId, string Data);
/// <summary>
/// Persistence log containing metadata necessary to revert changes made during State One.
/// </summary>
/// <param name="StartedAt">The UTC timestamp when the execution phase began.</param>
public record StateOneLog(DateTime StartedAt);

/// <summary>
/// Implements the initial activity in the routing slip, supporting both forward execution and backward compensation.
/// </summary>
public class StateOneActivity(IChaosProvider chaos, ILogger<StateOneActivity> logger)
    : IActivity<StateOneArgs, StateOneLog>
{
    /// <summary>
    /// Executes the primary logic for State One, including stability checks and logging.
    /// </summary>
    /// <param name="context">The execution context containing <see cref="StateOneArgs"/>.</param>
    /// <returns>A task representing the asynchronous execution result.</returns>
    public async Task<MassTransit.ExecutionResult> Execute(ExecuteContext<StateOneArgs> context)
    {
        logger.LogInformation("[FORWARD] State 1: Starting for {Id}", context.Arguments.TransactionId);

        // Validates system stability or injects synthetic failures for resilience testing
        chaos.EnsureStability("State 1 Execution");

        logger.LogInformation("[SUCCESS] State 1 Completed.");
        return context.Completed(new StateOneLog(DateTime.UtcNow));
    }

    /// <summary>
    /// Performs compensatory logic to undo changes if a subsequent activity in the routing slip fails.
    /// </summary>
    /// <param name="context">The compensation context containing the <see cref="StateOneLog"/>.</param>
    /// <returns>A task representing the asynchronous compensation result.</returns>
    public async Task<CompensationResult> Compensate(CompensateContext<StateOneLog> context)
    {
        logger.LogInformation("[BACKWARD] State 1: Undoing changes from {Time}", context.Log.StartedAt);

        try
        {
            // Compensation must also be resilient; check stability before attempting rollback
            chaos.EnsureStability("State 1 Compensation");
            logger.LogInformation("[CLEAN] State 1 Compensation successful.");
            return context.Compensated();
        }
        catch (ChaosException)
        {
            // Escalation point: If compensation fails, the system enters an inconsistent state requiring manual intervention
            logger.LogCritical("🚨 [ALERT] State 1 Compensation EXHAUSTED retries. Manual DB cleanup required!");
            throw;
        }
    }
}
