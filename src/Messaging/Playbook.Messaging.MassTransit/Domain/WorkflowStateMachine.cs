using MassTransit;

using Playbook.Messaging.MassTransit.Contracts;

namespace Playbook.Messaging.MassTransit.Domain;

public class WorkflowStateMachine : MassTransitStateMachine<WorkflowState>
{
    public State ProcessingState1 { get; private set; } = default!;
    public State ProcessingState2 { get; private set; } = default!;
    public State ProcessingState3 { get; private set; } = default!;
    public State RollingBackState2 { get; private set; } = default!;
    public State RollingBackState1 { get; private set; } = default!;
    public State Failed { get; private set; } = default!;

    public Event<StartWorkflow> StartWorkflow { get; private set; } = default!;
    public Event<State1Completed> State1Completed { get; private set; } = default!;
    public Event<State2Completed> State2Completed { get; private set; } = default!;
    public Event<State3Completed> State3Completed { get; private set; } = default!;
    public Event<State1Failed> State1Failed { get; private set; } = default!;
    public Event<State2Failed> State2Failed { get; private set; } = default!;
    public Event<State3Failed> State3Failed { get; private set; } = default!;
    public Event<State2Undone> State2Undone { get; private set; } = default!;
    public Event<State1Undone> State1Undone { get; private set; } = default!;

    public WorkflowStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Initially(
            When(StartWorkflow)
                .Then(context =>
                {
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
                .Publish(context => new UndoState1(context.Saga.CorrelationId, context.Message.ErrorMessage))
                .TransitionTo(RollingBackState1)
        );

        During(ProcessingState3,
            When(State3Completed)
                .Finalize(),
            When(State3Failed)
                .Publish(context => new UndoState2(context.Saga.CorrelationId, context.Message.ErrorMessage))
                .TransitionTo(RollingBackState2),
            When(State2Failed) // Out-of-order handling
                .Publish(context => new UndoState1(context.Saga.CorrelationId, "Late Failure"))
                .TransitionTo(RollingBackState1)
        );

        During(RollingBackState2,
            When(State2Undone)
                .Publish(context => new UndoState1(context.Saga.CorrelationId, "Sequential Rollback"))
                .TransitionTo(RollingBackState1)
        );

        During(RollingBackState1,
            When(State1Undone)
                .TransitionTo(Failed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
