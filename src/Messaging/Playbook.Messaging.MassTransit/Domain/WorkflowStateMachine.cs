using MassTransit;

using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Domain;

public class WorkflowStateMachine : MassTransitStateMachine<WorkflowState>
{
    // --- States ---
    public State ProcessingState1 { get; private set; } = default!;
    public State ProcessingState2 { get; private set; } = default!;
    public State ProcessingState3 { get; private set; } = default!;
    public State RollingBackState2 { get; private set; } = default!;
    public State RollingBackState1 { get; private set; } = default!;
    public State Failed { get; private set; } = default!;

    // --- Events & Commands ---
    public Event<StartWorkflow> StartWorkflow { get; private set; } = default!;
    public Event<State1Completed> State1Completed { get; private set; } = default!;
    public Event<State2Completed> State2Completed { get; private set; } = default!;
    public Event<State3Completed> State3Completed { get; private set; } = default!;

    // --- Failure Events ---
    public Event<State1Failed> State1Failed { get; private set; } = default!;
    public Event<State2Failed> State2Failed { get; private set; } = default!;
    public Event<State3Failed> State3Failed { get; private set; } = default!;

    // --- Undo Completion Events ---
    public Event<State2Undone> State2Undone { get; private set; } = default!;
    public Event<State1Undone> State1Undone { get; private set; } = default!;

    public WorkflowStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // 1. Initialization
        Initially(
            When(StartWorkflow)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.OrderName = context.Message.OrderName;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Starting Workflow: {context.Message.OrderName}");
                })
                .TransitionTo(ProcessingState1)
                .Publish(context => new ExecuteState1(context.Saga.CorrelationId))
        );

        // 2. State 1: The Foundation
        During(ProcessingState1,
            When(State1Completed)
                .TransitionTo(ProcessingState2)
                .Publish(context => new ExecuteState2(context.Saga.CorrelationId)),

            When(State1Failed)
                .Then(context => Console.WriteLine($"[SAGA] State 1 Failed: {context.Message.ErrorMessage}"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // 3. State 2: Mid-point (Requires Undo 1 if failed)
        During(ProcessingState2,
            When(State2Completed)
                .TransitionTo(ProcessingState3)
                .Publish(context => new ExecuteState3(context.Saga.CorrelationId)),

            When(State2Failed)
                .Then(context => Console.WriteLine($"[SAGA] State 2 Failed! Starting Rollback of State 1..."))
                .Publish(context => new UndoState1(context.Saga.CorrelationId, context.Message.ErrorMessage))
                .TransitionTo(RollingBackState1) // Wait for Undo 1 to finish
        );

        // 4. State 3: Final Step (Requires LIFO Rollback: Undo 2 -> Undo 1)
        During(ProcessingState3,
            When(State3Completed)
                .Then(context => Console.WriteLine("[SAGA] Workflow Finished Successfully!"))
                .Finalize(),

            When(State3Failed)
                .Then(context => Console.WriteLine($"[SAGA] State 3 Failed! Starting LIFO Rollback (2 -> 1)..."))
                .Publish(context => new UndoState2(context.Saga.CorrelationId, context.Message.ErrorMessage))
                .TransitionTo(RollingBackState2), // Wait for Undo 2 to finish

            // Handle accidental out-of-order State 2 failure
            When(State2Failed)
                .Then(context => Console.WriteLine($"[SAGA] Late State 2 Failure detected. Undoing State 1..."))
                .Publish(context => new UndoState1(context.Saga.CorrelationId, "Late Failure"))
                .TransitionTo(RollingBackState1)
        );

        // 5. The Rollback Chain (Waiting for janitors to finish)
        During(RollingBackState2,
            When(State2Undone)
                .Then(context => Console.WriteLine("[SAGA] State 2 Undone. Now undoing State 1..."))
                .Publish(context => new UndoState1(context.Saga.CorrelationId, "Sequential Rollback"))
                .TransitionTo(RollingBackState1)
        );

        During(RollingBackState1,
            When(State1Undone)
                .Then(context => Console.WriteLine("[SAGA] Rollback Complete. Saga Terminated."))
                .TransitionTo(Failed)
                .Finalize()
        );

        // Clean up from DB when Finalized state is reached
        SetCompletedWhenFinalized();
    }
}
