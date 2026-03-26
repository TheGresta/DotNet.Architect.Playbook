using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

namespace Playbook.Messaging.MassTransit.Courier.Courier.Activities.StateOne;

public record StateOneArgs(Guid TransactionId, string Data);
public record StateOneLog(DateTime StartedAt);

public class StateOneActivity(IChaosProvider chaos, ILogger<StateOneActivity> logger)
    : IActivity<StateOneArgs, StateOneLog>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<StateOneArgs> context)
    {
        logger.LogInformation("[FORWARD] State 1: Starting for {Id}", context.Arguments.TransactionId);

        // The Chaos Check
        chaos.EnsureStability("State 1 Execution");

        logger.LogInformation("[SUCCESS] State 1 Completed.");
        return context.Completed(new StateOneLog(DateTime.UtcNow));
    }

    public async Task<CompensationResult> Compensate(CompensateContext<StateOneLog> context)
    {
        logger.LogInformation("[BACKWARD] State 1: Undoing changes from {Time}", context.Log.StartedAt);

        try
        {
            chaos.EnsureStability("State 1 Compensation");
            logger.LogInformation("[CLEAN] State 1 Compensation successful.");
            return context.Compensated();
        }
        catch (ChaosException)
        {
            logger.LogCritical("🚨 [ALERT] State 1 Compensation EXHAUSTED retries. Manual DB cleanup required!");
            throw;
        }
    }
}
