namespace Playbook.Messaging.MassTransit.Courier.Contracts;

/// <summary>
/// Represents the shared context and metadata for a specific distributed workflow instance.
/// </summary>
/// <param name="TransactionId">The globally unique identifier for the business transaction.</param>
/// <param name="CustomerName">The name of the customer associated with this workflow execution.</param>
public record WorkflowContext(Guid TransactionId, string CustomerName);
