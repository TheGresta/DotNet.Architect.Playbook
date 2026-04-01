using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Services;

public interface IPasswordHasher
{
    /// <summary>
    /// Returns a strongly-typed PasswordHash Value Object.
    /// </summary>
    PasswordHash HashPassword(string password);

    /// <summary>
    /// Accepts the Value Object for verification.
    /// </summary>
    bool VerifyPassword(string password, PasswordHash storedHash);
}
