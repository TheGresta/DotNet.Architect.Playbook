using MassTransit;

using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Domain;

/// <summary>
/// Defines the state machine logic for managing a multi-stage distributed workflow with compensation (rollback) capabilities.
/// Coordinates the execution and failure handling of sequential processing states using MassTransit.
/// </summary>
public class WorkflowStateMachine : MassTransitStateMachine<WorkflowState>
{
    /// <summary>Gets the state representing the first stage of active processing.</summary>
    public State ProcessingState1 { get; private set; } = default!;
    /// <summary>Gets the state representing the second stage of active processing.</summary>
    public State ProcessingState2 { get; private set; } = default!;
    /// <summary>Gets the state representing the third stage of active processing.</summary>
    public State ProcessingState3 { get; private set; } = default!;
    /// <summary>Gets the state representing the rollback procedure for the second stage.</summary>
    public State RollingBackState2 { get; private set; } = default!;
    /// <summary>Gets the state representing the rollback procedure for the first stage.</summary>
    public State RollingBackState1 { get; private set; } = default!;
    /// <summary>Gets the final state representing a terminal failure of the workflow.</summary>
    public State Failed { get; private set; } = default!;

    /// <summary>Event triggered to initiate a new workflow instance.</summary>
    public Event<StartWorkflow> StartWorkflow { get; private set; } = default!;
    /// <summary>Event signaling successful completion of the first processing stage.</summary>
    public Event<State1Completed> State1Completed { get; private set; } = default!;
    /// <summary>Event signaling successful completion of the second processing stage.</summary>
    public Event<State2Completed> State2Completed { get; private set; } = default!;
    /// <summary>Event signaling successful completion of the third processing stage.</summary>
    public Event<State3Completed> State3Completed { get; private set; } = default!;
    /// <summary>Event signaling a failure during the first processing stage.</summary>
    public Event<State1Failed> State1Failed { get; private set; } = default!;
    /// <summary>Event signaling a failure during the second processing stage.</summary>
    public Event<State2Failed> State2Failed { get; private set; } = default!;
    /// <summary>Event signaling a failure during the third processing stage.</summary>
    public Event<State3Failed> State3Failed { get; private set; } = default!;
    /// <summary>Event confirming that the second stage has been successfully undone/compensated.</summary>
    public Event<State2Undone> State2Undone { get; private set; } = default!;
    /// <summary>Event confirming that the first stage has been successfully undone/compensated.</summary>
    public Event<State1Undone> State1Undone { get; private set; } = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStateMachine"/> class, 
    /// defining the correlations, states, and transition logic.
    /// </summary>
    public WorkflowStateMachine()
    {
        // Define which property on the Saga instance holds the current state.
        InstanceState(x => x.CurrentState);

        Initially(
            When(StartWorkflow)
                .Then(context =>
                {
                    // Initialize saga state data from the starting message.
                    context.Saga.OrderName = context.Message.OrderName;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .TransitionTo(ProcessingState1)
                .Publish(context => new ExecuteState1(context.Saga.CorrelationId))
        );

        During(ProcessingState1,
            When(State1Completed)
                .TransitionTo(ProcessingState2)
                .Publish(context => new ExecuteState2(context.Saga.CorrelationId)),
            When(State1Failed)
                .TransitionTo(Failed)
                .Finalize()
        );

        During(ProcessingState2,
            When(State2Completed)
                .TransitionTo(ProcessingState3)
                .Publish(context => new ExecuteState3(context.Saga.CorrelationId)),
            When(State2Failed)
                // Begin backward compensation by triggering the undo action for the previous successful stage.
                .Publish(context => new UndoState1(context.Saga.CorrelationId, context.Message.ErrorMessage))
                .TransitionTo(RollingBackState1)
        );

        During(ProcessingState3,
            When(State3Completed)
                .Finalize(),
            When(State3Failed)
                // Failure at State 3 requires sequential rollback of State 2 then State 1.
                .Publish(context => new UndoState2(context.Saga.CorrelationId, context.Message.ErrorMessage))
                .TransitionTo(RollingBackState2),
            When(State2Failed)
                // Handle edge cases where a failure message from a previous stage arrives late due to transport latency.
                .Publish(context => new UndoState1(context.Saga.CorrelationId, "Late Failure"))
                .TransitionTo(RollingBackState1)
        );

        During(RollingBackState2,
            When(State2Undone)
                // Continue the sequential compensation chain.
                .Publish(context => new UndoState1(context.Saga.CorrelationId, "Sequential Rollback"))
                .TransitionTo(RollingBackState1)
        );

        During(RollingBackState1,
            When(State1Undone)
                .TransitionTo(Failed)
                .Finalize()
        );

        // Ensures the saga instance is removed from the repository once it reaches a terminal state (Finalized).
        SetCompletedWhenFinalized();
    }
}
