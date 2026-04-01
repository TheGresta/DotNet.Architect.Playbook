using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Services;

/// <summary>
/// Domain service to ensure global uniqueness of identity identifiers.
/// This prevents race conditions and duplicate account creation.
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
