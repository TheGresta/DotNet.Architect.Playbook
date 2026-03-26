using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

namespace Playbook.Messaging.MassTransit.Courier.Courier.Activities.StateTwo;

public record StateTwoArgs(Guid TransactionId);
public record StateTwoLog(string ProcessId);

public class StateTwoActivity(IChaosProvider chaos, ILogger<StateTwoActivity> logger)
    : IActivity<StateTwoArgs, StateTwoLog>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<StateTwoArgs> context)
    {
        logger.LogInformation("[FORWARD] State 2: Processing data...");

        chaos.EnsureStability("State 2 Execution");

        logger.LogInformation("[SUCCESS] State 2 Completed.");
        return context.Completed(new StateTwoLog("PROC-99"));
    }

    public async Task<CompensationResult> Compensate(CompensateContext<StateTwoLog> context)
    {
        logger.LogInformation("[BACKWARD] State 2: Reverting Process {Id}", context.Log.ProcessId);
        return context.Compensated();
    }
}
