namespace Playbook.Messaging.MassTransit.Courier.Contracts;

// The unique identifier for our entire workflow
public record WorkflowContext(Guid TransactionId, string CustomerName);
