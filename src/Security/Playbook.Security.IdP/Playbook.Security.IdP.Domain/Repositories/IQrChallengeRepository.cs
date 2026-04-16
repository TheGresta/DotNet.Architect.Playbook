using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IQrChallengeRepository : IRepository<QrChallenge, QrChallengeId>
{
    /// <summary>
    /// Finds a challenge using the high-entropy secret code scanned by the mobile app.
    /// </summary>
    Task<QrChallenge?> GetBySecretCodeHashAsync(string secretCodeHash, CancellationToken ct = default);
}
