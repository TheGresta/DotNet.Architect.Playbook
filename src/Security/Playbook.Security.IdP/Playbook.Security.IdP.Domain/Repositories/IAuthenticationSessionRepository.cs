using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IAuthenticationSessionRepository : IRepository<AuthenticationSession, AuthenticationSessionId>
{
    /// <summary>
    /// Retrieves a session by its correlation ID (OIDC flow ID).
    /// </summary>
    Task<AuthenticationSession?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);

    /// <summary>
    /// Cleans up expired sessions.
    /// </summary>
    Task RemoveExpiredSessionsAsync(CancellationToken ct = default);
}
