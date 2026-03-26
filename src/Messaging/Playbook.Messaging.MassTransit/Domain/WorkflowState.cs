using MassTransit;

namespace Playbook.Messaging.MassTransit.Domain;

public class WorkflowState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;

    // Business Data preserved for "Undo" operations
    public string? OrderName { get; set; }
    public DateTime CreatedAt { get; set; }

    // Required by MassTransit for optimistic concurrency
    public int Version { get; set; }
}
