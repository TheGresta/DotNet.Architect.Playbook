using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IUserConsentRepository : IRepository<UserConsent, UserConsentId>
{
    /// <summary>
    /// Checks for an existing active consent for a specific User, Client,
    /// and redirect URI. The redirect URI must match exactly.
    /// </summary>
    Task<UserConsent?> GetActiveConsentAsync(
        UserId userId,
        string clientId,
        string redirectUri,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all active consents for a user (used for the 'Authorized Apps' UI).
    /// </summary>
    Task<IEnumerable<UserConsent>> GetActiveConsentsByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);
}
