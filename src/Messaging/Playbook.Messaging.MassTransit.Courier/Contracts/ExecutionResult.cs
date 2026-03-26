namespace Playbook.Messaging.MassTransit.Courier.Contracts;

/// <summary>
/// Encapsulates the result of a workflow step execution for API response mapping.
/// </summary>
/// <param name="TrackingNumber">The MassTransit tracking number for the routing slip.</param>
/// <param name="Status">The current state or outcome of the execution.</param>
public record ExecutionResult(Guid TrackingNumber, string Status);
