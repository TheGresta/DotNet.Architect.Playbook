using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IRecoveryCodeRepository : IRepository<RecoveryCode, RecoveryCodeId>
{
    /// <summary>
    /// Atomically marks a recovery code as consumed **only if it is still unconsumed**.
    /// Returns <c>true</c> when the row was updated; <c>false</c> when it was already consumed
    /// (either by a concurrent request or a previous call).
    /// </summary>
    Task<bool> ConsumeAtomicAsync(
        RecoveryCodeId id,
        DateTimeOffset utcNow,
        CancellationToken ct = default);
}
