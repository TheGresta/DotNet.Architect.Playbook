using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Provides access to the identity of the user performing the current operation.
/// This interface is implemented in the Infrastructure layer (via HttpContext or System Identity).
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// The unique identifier of the current user. Returns null if the request is anonymous.
    /// </summary>
    UserId? UserId { get; }

    /// <summary>
    /// Indicates if the current request is being performed by an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// For background tasks or system processes, identifies the system actor name.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// The IP address of the current requestor for audit and risk scoring.
    /// </summary>
    string? IpAddress { get; }
}
