namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

/// <summary>
/// Indicates that the request should be captured by the security audit log.
/// </summary>
public interface IAuditableRequest
{
    // The request defines how it should be described in the logs
    string GetAuditSummary();

    // The request defines which resource it is touching
    string ResourceName { get; }
    string? ResourceId { get; }
}

