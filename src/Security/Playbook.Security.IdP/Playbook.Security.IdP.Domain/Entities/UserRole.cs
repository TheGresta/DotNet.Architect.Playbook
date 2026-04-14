using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Join entity representing a time-bound role assignment for a user.
///
/// Design decisions:
/// - Supports expiring role assignments (e.g., temporary elevated access).
/// - GrantedBy captures the admin who made the assignment for the audit trail.
/// - IsExpired() is a pure domain query — no infrastructure dependency.
/// </summary>
public sealed class UserRole : Entity<UserRoleId>
{
    public UserId UserId { get; private set; } = null!;
    public RoleId RoleId { get; private set; } = null!;
    public UserId GrantedBy { get; private set; } = null!;
    public DateTime GrantedAt { get; private set; }

    /// <summary>Null = permanent assignment.</summary>
    public DateTime? ExpiresAt { get; private set; }

    // ORM constructor
    private UserRole() { }

    internal UserRole(UserId userId, RoleId roleId, UserId grantedBy, DateTime? expiresAt = null)
    {
        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new Exceptions.DomainException(
                "Role assignment expiry must be in the future.", "INVALID_ROLE_EXPIRY");

        UserId = userId;
        RoleId = roleId;
        GrantedBy = grantedBy;
        GrantedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired() =>
        ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    public bool StillActive() => !IsExpired();
}
