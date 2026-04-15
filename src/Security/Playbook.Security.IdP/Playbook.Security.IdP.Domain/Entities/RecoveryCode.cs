using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// A single hashed MFA backup recovery code.
/// Each code can only be consumed once.
/// </summary>
public sealed class RecoveryCode : Entity<RecoveryCodeId>
{
    public UserId UserId { get; private set; } = null!;
    public PasswordHash CodeHash { get; private set; } = null!;
    public bool IsConsumed { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Managed exclusively by EF Core.
    /// Ensures a stale read is rejected at the DB write boundary.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    // ORM constructor
    private RecoveryCode() { }

    internal RecoveryCode(UserId userId, PasswordHash codeHash)
    {
        Id = RecoveryCodeId.New();
        UserId = userId;
        CodeHash = codeHash;
        IsConsumed = false;
    }

    /// <param name="utcNow">
    /// Injected by the command handler via ISystemClock – never call DateTime.UtcNow here.
    /// </param>
    internal void Consume(DateTimeOffset utcNow)
    {
        if (IsConsumed)
            throw new DomainException("Recovery code has already been used.", "RECOVERY_CODE_CONSUMED");

        IsConsumed = true;
        ConsumedAt = utcNow.UtcDateTime;
    }
}
