using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Services;

/// <summary>
/// Advisory-only pre-check for identity identifier uniqueness.
/// These methods are NOT a security gate and do NOT prevent race conditions (TOCTOU).
/// They are intended solely for fast UX feedback (e.g., inline form validation).
///
/// TRUE uniqueness MUST be enforced at the persistence layer via unique database indexes
/// (UIX_Users_Email, UIX_Users_Username). Callers must handle write-time conflicts
/// (DomainException with codes EMAIL_CONFLICT / USERNAME_CONFLICT) on every
/// create/update operation and must NOT rely solely on these pre-checks for correctness.
/// </summary>
public interface IUniqueIdentityChecker
{
    /// <summary>
    /// Checks if a username is already taken across the entire system.
    /// </summary>
    Task<bool> IsUsernameUniqueAsync(Username username, CancellationToken ct = default);

    /// <summary>
    /// Checks if an email is already registered.
    /// </summary>
    Task<bool> IsEmailUniqueAsync(Email email, CancellationToken ct = default);
}
