using MassTransit;

namespace Playbook.Messaging.MassTransit.Saga.Domain;

/// <summary>
/// Represents the persisted state of a saga instance within the workflow.
/// Implements <see cref="SagaStateMachineInstance"/> for MassTransit state machine integration
/// and <see cref="ISagaVersion"/> to support optimistic concurrency.
/// </summary>
public class WorkflowState : SagaStateMachineInstance, ISagaVersion
{
    /// <summary>
    /// Gets or sets the unique identifier used to correlate messages to this specific saga instance.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the current state of the state machine as a string.
    /// Maps to the <see cref="State"/> definitions in the state machine.
    /// </summary>
    public string CurrentState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the human-readable name or identifier for the order associated with this workflow.
    /// </summary>
    public string? OrderName { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the workflow instance was initially created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the version of the saga instance for optimistic concurrency control in the database.
    /// </summary>
    public int Version { get; set; }
}
