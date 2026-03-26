using MassTransit;

namespace Playbook.Messaging.MassTransit.Domain;

public class WorkflowState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;
    public string? OrderName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; }
}
