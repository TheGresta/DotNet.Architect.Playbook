namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

/// <summary>
/// Indicates that the request should be captured by the security audit log.
/// </summary>
public interface IAuditableRequest
{
    /// <summary>
    /// Allows the request to specify if it should be audited (defaults to true).
    /// </summary>
    bool ShouldAudit => true;
}

