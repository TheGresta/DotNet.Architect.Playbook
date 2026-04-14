using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Application.Abstractions.Security;

/// <summary>
/// Provides contextual information about the current request, user, and device.
/// This will be implemented in the Infrastructure layer (e.g., reading from HttpContext/JWT).
/// </summary>
public interface IRequestContext
{
    UserId? UserId { get; }
    string? DeviceId { get; }
    string IpAddress { get; }
    string UserAgent { get; }
    string CorrelationId { get; }
}
