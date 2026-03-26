namespace Playbook.Messaging.MassTransit.Courier.Contracts;

// Standard result for our API to return
public record ExecutionResult(Guid TrackingNumber, string Status);
